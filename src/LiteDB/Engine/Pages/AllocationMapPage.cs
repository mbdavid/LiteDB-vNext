namespace LiteDB.Engine;

/// <summary>
/// Represent a single allocation map page with 1.632 extends and 13.056 pages pointer
/// </summary>
internal class AllocationMapPage : BasePage
{
    private readonly Queue<int> _emptyExtends;

    private readonly int _allocationMapID;

    private bool _isDirty = false;

    /// <summary>
    /// AllocationMap is dirty is marked when content change (not only when have _writer != null)
    /// </summary>
    public override bool IsDirty => _isDirty;

    /// <summary>
    /// Get how many extends exists in this page
    /// </summary>
    public int ExtendsCount => AM_EXTEND_COUNT - _emptyExtends.Count;

    /// <summary>
    /// Create a new AllocationMapPage
    /// </summary>
    public AllocationMapPage(uint pageID, PageBuffer writeBuffer)
        : base(pageID, PageType.AllocationMap, writeBuffer)
    {
        // get allocationMapID
        _allocationMapID = GetAllocationMapID(pageID);

        // fill all queue as empty extends (use ExtendID)
        _emptyExtends = new Queue<int>(Enumerable.Range(0, AM_EXTEND_COUNT)
            .Select(x => x * _allocationMapID));
    }

    /// <summary>
    /// Load AllocationMap from buffer memory
    /// </summary>
    public AllocationMapPage(PageBuffer buffer)
        : base(buffer, null)
    {
        // get allocationMapID
        _allocationMapID = GetAllocationMapID(this.PageID);

        // for AllocationMapPage i will a single buffer to read (on database load) and write (on database close)
        _writeBuffer = buffer;

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

        var span = _readBuffer.AsSpan();

        ENSURE(_emptyExtends.Count == 0, "empty extends will be loaded here and can't have any page before here");

        for (var i = 0; i < AM_EXTEND_COUNT; i++)
        {
            var position = PAGE_HEADER_SIZE + (i * AM_EXTEND_SIZE);

            // get extendID
            var extendID = i * _allocationMapID;

            // check if empty colID (means empty extend)
            var colID = span[position];

            if (colID == 0)
            {
                DEBUG(span[position..(position + AM_BYTES_PER_EXTEND)].IsFullZero(), $"all page extend allocation map should be empty at {position}");

                _emptyExtends.Enqueue(extendID);
            }
            else
            {
                // get 3 bytes from extend to read 8 sequencial pages free spaces
                var pagesBytes = span[(position + 1)..(position + AM_BYTES_PER_EXTEND - 2)];

                // get free page lists from collection
                var freePages = collectionFreePages[colID];

                // read all extend pages and add to collection free pages
                this.ReadExtend(freePages, extendID, pagesBytes);
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

        this.InitializeWrite();

        // get a empty extend on this page
        var extendID = _emptyExtends.Dequeue();

        // get first PageID
        var firstPageID = GetBlockPageID(extendID, 0);
        var pageID = firstPageID;

        // add all pages as emptyPages in freePages list
        for (var i = 0; i < AM_EXTEND_SIZE; i++)
        {
            freePages.EmptyPages.Enqueue(pageID);

            pageID++;
        }

        var span = _writeBuffer!.Value.AsSpan();

        // get extend position inside this buffer
        var colIndex = GetExtendPosition(extendID);

        // mark buffer write with this collection ID
        span[colIndex] = colID;

        _isDirty = true;

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
        var pageBytes = _writeBuffer!.Value.AsSpan(position + 1, position + AM_BYTES_PER_EXTEND);

        // mark page as dirty (in map page this should be manual)
        _isDirty = true;

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

    #region Static Helpers

    /// <summary>
    /// Returns a AllocationMapID from a allocation map page number. Must return 0, 1, 2, 3
    /// </summary>
    public static int GetAllocationMapID(uint pageID)
    {
        return (int)(pageID - AM_FIRST_PAGE_ID) % AM_EXTEND_COUNT;
    }

    /// <summary>
    /// Get PageID from a block page based on ExtendID (0, 1, ..) and PageIndex (0-7)
    /// </summary>
    public static uint GetBlockPageID(int extendID, int pageIndex)
    {
        return 0; //TODO: cassiano
    }

    /// <summary>
    /// Get a extend position in current buffer based on ExtendID (32...8156 = 8160-4)
    /// </summary>
    public static byte GetExtendPosition(int extendID)
    {
        throw new NotImplementedException();
    }

    #endregion
}
