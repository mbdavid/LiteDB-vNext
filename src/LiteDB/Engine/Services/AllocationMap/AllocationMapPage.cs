namespace LiteDB.Engine;

/// <summary>
/// Represent a single allocation map page with 1.632 extends and 13.056 pages pointer
/// Each extend represent 8 pages at same collection. Each extend use 4 bytes (int32)
/// 
///  01234567   01234567   01234567   01234567
/// [________] [________] [________] [________]
///  ColID      00011122   23334445   55666777
/// </summary>
internal class AllocationMapPage
{
    private readonly int _allocationMapID;

    private readonly PageBuffer _page;

    private readonly uint[] _extendValues = new uint[AM_EXTEND_COUNT];

    private readonly Queue<int> _emptyExtends;

    public int AllocationMapID => _allocationMapID;

    public PageBuffer Page => _page;

    /// <summary>
    /// Create a new AllocationMapPage
    /// </summary>
    public AllocationMapPage(int pageID, PageBuffer page)
    {
        // keep pageBuffer instance
        _page = page;

        // update page header
        _page.Header.PageID = pageID;
        _page.Header.PageType = PageType.AllocationMap;

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
        // keep pageBuffer instance
        _page = page;

        // get allocationMapID
        _allocationMapID = GetAllocationMapID(page.Header.PageID);

        // create an empty list of extends
        _emptyExtends = new Queue<int>(AM_EXTEND_COUNT);

        var span = _page.AsSpan(PAGE_HEADER_SIZE);

        // read all page
        for(var i = 0; i < AM_EXTEND_COUNT; i++)
        {
            var value = span[(i * AM_BYTES_PER_EXTEND)..].ReadExtendValue();

            if (value == 0)
            {
                _emptyExtends.Enqueue(i);
            }

            _extendValues[i] = value;
        }
    }

    /// <summary>
    /// Get a extend value from this page based on extend index
    /// </summary>
    public uint GetExtendValue(int extendIndex) => _extendValues[extendIndex];

    public void RestoreExtendValue(int extendIndex, uint value)
    {
        _page.IsDirty = true;

        _extendValues[extendIndex] = value;
    }

    /// <summary>
    /// Read all extendValues to return the first extendIndex with avaliable space. Returns pageIndex for this pag
    /// Returns -1 if has no extend with this condition.
    /// </summary>
    public (int extendIndex, int pageIndex, bool isNew) GetFreeExtend(byte colID, PageType type, int length)
    {
        for (var i = 0; i < AM_EXTEND_COUNT; i++)
        {
            // get extend value as uint
            var extendValue = _extendValues[i];

            var (pageIndex, isNew) = HasFreeSpaceInExtend(extendValue, colID, type, length);

            if (pageIndex != -1)
            {
                return (i, pageIndex, isNew);
            }
        }

        return (-1, 0, false);
    }

    /// <summary>
    /// Create a new extend (if contains empty extends) and return new extendIndex or -1 if has no more empty extends
    /// </summary>
    public int CreateNewExtend(byte colID)
    {
        if (_emptyExtends.Count == 0) return -1;

        // get a empty extend on this page
        var extendIndex = _emptyExtends.Dequeue();

        // update extend value with only colID value in first 1 byte (shift 3 bytes)
        _extendValues[extendIndex] = (uint)(colID << 24);

        // mark page as dirty
        _page.IsDirty = true;

        return extendIndex;
    }

    /// <summary>
    /// Update extend value based on extendIndex (0-2039) and pageIndex (0-7). PageValue should be 0-7
    /// </summary>
    public void UpdateExtendPageValue(int extendIndex, int pageIndex, uint pageValue)
    {
        // get extend value from array
        var value = _extendValues[extendIndex];

        // update value (3 bits) according pageIndex (can update 1 or 2 bytes)
        var extendValue = pageIndex switch
        {
            0 => (value & 0b11111111_00011111_11111111_11111111) | (pageValue << 21),
            1 => (value & 0b11111111_11100011_11111111_11111111) | (pageValue << 18),
            2 => (value & 0b11111111_11111100_01111111_11111111) | (pageValue << 15),
            3 => (value & 0b11111111_11111111_10001111_11111111) | (pageValue << 12),
            4 => (value & 0b11111111_11111111_11110001_11111111) | (pageValue << 9),
            5 => (value & 0b11111111_11111111_11111110_00111111) | (pageValue << 6),
            6 => (value & 0b11111111_11111111_11111111_11000111) | (pageValue << 3),
            7 => (value & 0b11111111_11111111_11111111_11111000) | (pageValue),
            _ => throw new InvalidOperationException()
        };

        // update extend array value
        _extendValues[extendIndex] = extendValue;

        // mark page as dirty (in map page this should be manual)
        _page.IsDirty = true;

#if DEBUG
        // in debug mode, I will update not only _extendValues array, but also PageBuffer array
        // in release mode, this update will occurs only in UpdatePageBuffer(), on Shutdown process
        var pos = extendIndex * AM_BYTES_PER_EXTEND;
        var span = _page.AsSpan(PAGE_HEADER_SIZE);
        span[pos..].WriteExtendValue(extendValue);
#endif
    }


    /// <summary>
    /// Get PageID from a block page based on ExtendIndex (0-2039) and PageIndex (0-7)
    /// </summary>
    public int GetBlockPageID(int extendIndex, int pageIndex)
    {
        return (_allocationMapID * AM_PAGE_STEP +
             extendIndex * AM_EXTEND_SIZE +
             pageIndex + 1);
    }

    /// <summary>
    /// Update PageBuffer instance with changed arrays. Returns false if there is no changes
    /// </summary>
    public bool UpdatePageBuffer()
    {
        if (!_page.IsDirty) return false;

        var span = _page.AsSpan(PAGE_HEADER_SIZE);
        var pos = 0;

        for(var i = 0; i < AM_EXTEND_COUNT; i++)
        {
            span[pos..].WriteExtendValue(_extendValues[i]);

            pos += AM_BYTES_PER_EXTEND;
        }

        return true;
    }

    #region Static Helpers

    /// <summary>
    /// Check if extend value contains a page that fit on request (type/length)
    /// Returns pageIndex if found or -1 if this extend has no space. Returns if page is new (empty)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int pageIndex, bool isNew) HasFreeSpaceInExtend(uint extendValue, byte colID, PageType type, int length)
    {
        // extendValue (colID + 8 pages values)

        //  01234567   01234567   01234567   01234567
        // [________] [________] [________] [________]
        //  ColID      00011122   23334445   55666777

        // 000 - empty
        // 001 - data large
        // 010 - data medium
        // 011 - data small
        // 100 - index 
        // 101 - data full
        // 110 - index full
        // 111 - reserved


        // check for same colID
        if (colID != extendValue >> 24) return (-1, false);

        uint result;

        if (type == PageType.Data && length <= AM_DATA_PAGE_SPACE_SMALL)
        {
            // 000 - empty
            // 001 - large
            // 010 - medium
            // 011 - small

            result = (extendValue & 0b00000000_100_100_100_100_100_100_100_100) 
                ^ 0b00000000_100_100_100_100_100_100_100_100;
        }
        else if (type == PageType.Data && length <= AM_DATA_PAGE_SPACE_MEDIUM)
        {
            // 000 - empty
            // 001 - large
            // 010 - medium
            var notA = (extendValue & 0b00000000_100_100_100_100_100_100_100_100) ^ 0b00000000_100_100_100_100_100_100_100_100;
            var notB = (extendValue & 0b00000000_010_010_010_010_010_010_010_010) ^ 0b00000000_010_010_010_010_010_010_010_010;
            var notC = (extendValue & 0b00000000_001_001_001_001_001_001_001_001) ^ 0b00000000_001_001_001_001_001_001_001_001;

            notB <<= 1;
            notC <<= 2;

            result = (notA & notC) | (notA & notB);
        }
        else if (type == PageType.Data && length <= AM_DATA_PAGE_SPACE_LARGE)
        {
            // 000 - empty
            // 001 - large
            var notA = (extendValue & 0b00000000_100_100_100_100_100_100_100_100) ^ 0b00000000_100_100_100_100_100_100_100_100;
            var notB = (extendValue & 0b00000000_010_010_010_010_010_010_010_010) ^ 0b00000000_010_010_010_010_010_010_010_010;

            notB <<= 1;

            result = (notA & notB);
        }
        else if (type == PageType.Data && length > AM_DATA_PAGE_SPACE_LARGE)
        {
            // 000 - empty
            var notA = (extendValue & 0b00000000_100_100_100_100_100_100_100_100) ^ 0b00000000_100_100_100_100_100_100_100_100;
            var notB = (extendValue & 0b00000000_010_010_010_010_010_010_010_010) ^ 0b00000000_010_010_010_010_010_010_010_010;
            var notC = (extendValue & 0b00000000_001_001_001_001_001_001_001_001) ^ 0b00000000_001_001_001_001_001_001_001_001;

            notB <<= 1;
            notC <<= 2;

            result = (notA & notB & notC);
        }
        else if (type == PageType.Index)
        {
            // 000 - empty
            // 100 - index
            var notB = (extendValue & 0b00000000_010_010_010_010_010_010_010_010) ^ 0b00000000_010_010_010_010_010_010_010_010;
            var notC = (extendValue & 0b00000000_001_001_001_001_001_001_001_001) ^ 0b00000000_001_001_001_001_001_001_001_001;

            notB <<= 1;
            notC <<= 2;

            result = (notB & notC);
        }
        else
        {
            return (-1, false);
        }

        if (result > 0)
        {
            var pageIndex = result switch
            {
                <= 31 => 7,
                <= 255 => 6,
                <= 2047 => 5,
                <= 16383 => 4,
                <= 131071 => 3,
                <= 1048575 => 2,
                <= 8388607 => 1,
                <= 67108863 => 0,
                _ => throw new NotSupportedException()
            };

            var isEmpty = (extendValue & (0b111 << ((7 - pageIndex) * 3))) == 0;

            return (pageIndex, isEmpty); //sombrio (como verificar se meu pageIndex é uma EmptyPage)
        }
        else
        {
            return (-1, false);
        }
    }

    /// <summary>
    /// Returns a AllocationMapID from a allocation map pageID. Must return 0, 1, 2, 3
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAllocationMapID(int pageID)
    {
        return (pageID - AM_FIRST_PAGE_ID) % AM_EXTEND_COUNT;
    }

    /// <summary>
    /// Get a value (0-7) thats represent diferent page types/avaiable spaces
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetAllocationPageValue(ref PageHeader header)
    {
        return (header.PageType, header.FreeBytes) switch
        {
            (_, PAGE_CONTENT_SIZE) => 0, // empty page, no matter page type
            (PageType.Data, >= AM_DATA_PAGE_SPACE_LARGE and < PAGE_CONTENT_SIZE) => 1,
            (PageType.Data, >= AM_DATA_PAGE_SPACE_MEDIUM) => 2,
            (PageType.Data, >= AM_DATA_PAGE_SPACE_SMALL) => 3,
            (PageType.Index, >= AM_INDEX_PAGE_SPACE) => 4,
            (PageType.Data, < AM_DATA_PAGE_SPACE_SMALL) => 5,
            (PageType.Index, < AM_INDEX_PAGE_SPACE) => 6,
            _ => throw new NotSupportedException()
        };
    }

    #endregion
}
