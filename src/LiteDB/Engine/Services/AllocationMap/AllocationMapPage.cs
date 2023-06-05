namespace LiteDB.Engine;

/// <summary>
/// Represent a single allocation map page with 1.632 extends and 13.056 pages pointer
/// Each extend represent 8 pages at same collection. Each extend use 4 bytes
/// 
///  01234567   01234567   01234567   01234567
/// [________] [________] [________] [________]
///  ColID      00011122   23334445   55666777
/// </summary>
internal class AllocationMapPage
{
    private readonly Queue<int> _emptyExtends;

    private readonly int _allocationMapID;

    private readonly PageBuffer _page;

    public int PageID => _page.Header.PageID;

    /// <summary>
    /// Create a new AllocationMapPage
    /// </summary>
    public AllocationMapPage(int pageID, PageBuffer page)
    {
        _page = page;

        // update page header
        page.Header.PageID = pageID;
        page.Header.PageType = PageType.AllocationMap;

        // get allocationMapID
        _allocationMapID = GetAllocationMapID(pageID);

        // fill all queue as empty extends (use ExtendIndex)
        _emptyExtends = new Queue<int>(Enumerable.Range(0, AM_EXTEND_COUNT - 1));
    }

    /// <summary>
    /// Load AllocationMap from buffer memory
    /// </summary>
    public AllocationMapPage(PageBuffer page)
    {
        _page = page;

        // get allocationMapID
        _allocationMapID = GetAllocationMapID(page.Header.PageID);

        // create an empty list of extends
        _emptyExtends = new Queue<int>(AM_EXTEND_COUNT);
    }

    /// <summary>
    /// Read all allocation map page and populate collectionFreePages from AllocationMap service instance
    /// </summary>
    public void ReadAllocationMap(CollectionFreePages[] collectionFreePages)
    {
        // if this page contais all empty extends, there is no need to read all buffer
        if (_emptyExtends.Count == AM_EXTEND_COUNT) return;


        ENSURE(_emptyExtends.Count == 0, "empty extends will be loaded here and can't have any page before here");

        for (var extendIndex = 0; extendIndex < AM_EXTEND_COUNT; extendIndex++)
        {
            // extend position on buffer
            var position = PAGE_HEADER_SIZE + (extendIndex * AM_BYTES_PER_EXTEND);

            var span = _page.AsSpan();

            // check if empty colID (means empty extend)
            var colID = span[position];

            if (colID == 0)
            {
                DEBUG(span.Slice(position, AM_BYTES_PER_EXTEND).IsFullZero(), $"all page extend allocation map should be empty at {position}");

                _emptyExtends.Enqueue(extendIndex);
            }
            // for all other (except $master collection)
            else
            {
                // get 3 bytes from extend to read 8 sequencial pages free spaces
                var pagesBytes = span.Slice(position + 1, AM_BYTES_PER_EXTEND - 1);

                // get (or create) free page lists from collection
                var freePages = collectionFreePages[colID] =
                    collectionFreePages[colID] ?? new CollectionFreePages();

                // read all extend pages and add to collection free pages
                this.ReadExtend(freePages, extendIndex, pagesBytes);
            }
        }
    }

    /// <summary>
    /// Read a single extend with 8 pages in 3 bytes. Add pageID into collectionFreePages
    /// </summary>
    private void ReadExtend(CollectionFreePages freePages, int extendID, Span<byte> pageBytes)
    {
        // do not use array to avoid memory allocation on heap

        var page0 = (pageBytes[0] & 0b111_000_00) >> 5; // first 3 bits from byte 0
        var page1 = (pageBytes[0] & 0b000_111_00) >> 2; // second 3 bits from byte 0

        var page2 = ((pageBytes[0] & 0b000_000_11) << 1) | // last 2 bits from byte 0
                    ((pageBytes[1] & 0b1_000_000_0) >> 7); // plus first 1 bit from byte 1

        var page3 = (pageBytes[1] & 0b0_111_000_0) >> 4; // bits position 1 to 3 from byte 1
        var page4 = (pageBytes[1] & 0b0_000_111_0) >> 1; // bits position 4 to 6 from byte 1

        var page5 = ((pageBytes[1] & 0b0_000_000_1) << 2) | // last 1 bit from byte 1
                    ((pageBytes[2] & 0b11_000_000) >> 6); // plus first 2 bits from byte 2

        var page6 = (pageBytes[2] & 0b00_111_000) >> 3; // bits position 2 to 4 from byte 2
        var page7 = (pageBytes[2] & 0b00_000_111); // last 3 bits from byte 2

        Add(0, page0);
        Add(1, page1);
        Add(2, page2);
        Add(3, page3);
        Add(4, page4);
        Add(5, page5);
        Add(6, page6);
        Add(7, page7);

        void Add(int index, int pageData)
        {
            // get pageID based on extendID
            var pageID = GetBlockPageID(extendID, index);

            var list = pageData switch
            {
                0b000 => freePages.EmptyPages,
                0b001 => freePages.DataPagesLarge,
                0b010 => freePages.DataPagesMedium,
                0b011 => freePages.DataPagesSmall,
                0b100 => null, // data full
                0b101 => freePages.IndexPages,
                0b110 => null, // index full
                0b111 => null, // reserved
                _ => null
            };

            list?.Enqueue(pageID);
        }
    }

    /// <summary>
    /// Create a new extend for a collection. Remove this free extend from emptyExtends
    /// If there is no more free extends, returns false. Populate freePages with new 8 empty pages for this collection
    /// </summary>
    public bool CreateNewExtend(byte colID, CollectionFreePages freePages)
    {
        if (_emptyExtends.Count == 0) return false;

        // get a empty extend on this page
        var extendIndex = _emptyExtends.Dequeue();

        // get first PageID
        var firstPageID = GetBlockPageID(extendIndex, 0);

        // add all pages as emptyPages in freePages list
        for (var i = 0; i < AM_EXTEND_SIZE; i++)
        {
            freePages.EmptyPages.Enqueue(firstPageID + i);
        }

        // get page span
        var span = _page.AsSpan();

        // get extend position inside this buffer
        var colPosition = PAGE_HEADER_SIZE + (extendIndex * AM_BYTES_PER_EXTEND);

        // mark buffer write with this collection ID
        span[colPosition] = colID;

        // mark buffer as dirty
        _page.IsDirty = true;

        return true;
    }

    /// <summary>
    /// Update map buffer based on extendIndex (0-2039) and pageIndex (0-7). Value shloud be 0-7
    /// This method update 1 ou 2 bytes acording with pageIndex
    /// </summary>
    public void UpdateMap(int extendIndex, int pageIndex, byte value)
    {
        // get extend start posistion on buffer
        var position = PAGE_HEADER_SIZE + (extendIndex *  AM_BYTES_PER_EXTEND);

        // get 3 pageBytes
        var pageBytes = _page.AsSpan(position + 1, AM_BYTES_PER_EXTEND - 1);

        // mark buffer as dirty (in map page this should be manual)
        _page.IsDirty = true;

        // update value (3 bits) according pageIndex (can update 1 or 2 bytes)
        switch (pageIndex)
        {
            case 0:
                pageBytes[0] = (byte)((pageBytes[0] & 0b000_111_11) | (value << 5));
                break;
            case 1:
                pageBytes[0] = (byte)((pageBytes[0] & 0b111_000_11) | (value << 2));
                break;
            case 2:
                pageBytes[0] = (byte)((pageBytes[0] & 0b111_111_00) | (value >> 1));
                pageBytes[1] = (byte)((pageBytes[1] & 0b0_111_111_1) | (value << 7));
                break;
            case 3:
                pageBytes[1] = (byte)((pageBytes[1] & 0b1_000_111_1) | (value << 4));
                break;
            case 4:
                pageBytes[1] = (byte)((pageBytes[1] & 0b1_111_000_1) | (value << 1));
                break;
            case 5:
                pageBytes[1] = (byte)((pageBytes[1] & 0b1_111_111_0) | (value >> 2));
                pageBytes[2] = (byte)((pageBytes[2] & 0b00_111_111) | (value << 6));
                break;
            case 6:
                pageBytes[2] = (byte)((pageBytes[2] & 0b11_000_111) | (value << 3));
                break;
            case 7:
                pageBytes[2] = (byte)((pageBytes[2] & 0b11_111_000) | value);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Get PageID from a block page based on ExtendID (0, 1, ..) and PageIndex (0-7)
    /// </summary>
    public int GetBlockPageID(int extendIndex, int pageIndex)
    {
        return (_allocationMapID * AM_PAGE_STEP +
             extendIndex * AM_EXTEND_SIZE +
             pageIndex + 1);
    }

    #region Static Helpers

    /// <summary>
    /// Returns a AllocationMapID from a allocation map pageID. Must return 0, 1, 2, 3
    /// </summary>
    public static int GetAllocationMapID(int pageID)
    {
        return (pageID - AM_FIRST_PAGE_ID) % AM_EXTEND_COUNT;
    }

    #endregion
}
