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

    private bool _isDirty = false;

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
            var value = span[(i * AM_BYTES_PER_EXTEND)..].ReadUInt32();

            if (value == 0)
            {
                _emptyExtends.Enqueue(i);
            }

            _extendValues[i] = value;
        }
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
        _isDirty = true;

        return extendIndex;
    }

    /// <summary>
    /// Update extend value based on extendIndex (0-2039) and pageIndex (0-7). PageValue should be 0-7
    /// </summary>
    public void UpdateMap(int extendIndex, int pageIndex, int pageValue)
    {
        // get extend value from array
        var value = _extendValues[extendIndex];

        // update value (3 bits) according pageIndex (can update 1 or 2 bytes)
        var extendValue = pageIndex switch
        {
            0 => (value & 0b11111111_00011111_11111111_11111111) | (uint)(pageValue << 21),
            1 => (value & 0b11111111_11100011_11111111_11111111) | (uint)(pageValue << 18),
            2 => (value & 0b11111111_11111100_01111111_11111111) | (uint)(pageValue << 15),
            3 => (value & 0b11111111_11111111_10001111_11111111) | (uint)(pageValue << 12),
            4 => (value & 0b11111111_11111111_11110001_11111111) | (uint)(pageValue << 9),
            5 => (value & 0b11111111_11111111_11111110_00111111) | (uint)(pageValue << 6),
            6 => (value & 0b11111111_11111111_11111111_11000111) | (uint)(pageValue << 3),
            7 => (value & 0b11111111_11111111_11111111_11111000) | (uint)(pageValue),
            _ => throw new InvalidOperationException()
        };

        _extendValues[extendIndex] = extendValue;

        // mark page as dirty (in map page this should be manual)
        _isDirty = true;
    }

    /// <summary>
    /// Read all extendValues to return the first extendIndex with avaliable space 
    /// Returns -1 if has no extend with this condition.
    /// </summary>
    public int GetFreeExtend(byte colID, PageType type, int length)
    {
        for (var i = 0; i < AM_EXTEND_COUNT; i++)
        {
            // get extend value as uint
            var extendValue = _extendValues[i];

            if (HasFreeSpaceInExtend(extendValue, colID, type, length))
            {
                return i;
            }
        }

        return -1;
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
    /// Update PageBuffer instance with changed arrays. Returns false if there is no change
    /// </summary>
    public bool UpdatePageBuffer()
    {
        if (!_isDirty) return false;

        var span = _page.AsSpan(PAGE_HEADER_SIZE);
        var pos = 0;

        for(var i = 0; i < AM_EXTEND_COUNT; i++)
        {
            span[pos..].WriteUInt32(_extendValues[i]);

            pos += AM_BYTES_PER_EXTEND;
        }

        return true;
    }

    #region Static Helpers

    /// <summary>
    /// Returns true is extend is from same colID and has at least 1 page with space avaiable
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFreeSpaceInExtend(uint extendValue, byte colID, PageType type, int length)
    {
        //  01234567   01234567   01234567   01234567
        // [________] [________] [________] [________]
        //  ColID      00011122   23334445   55666777

        // check for same colID
        if (colID != extendValue >> 24) return false;

        if (type == PageType.Data && length < AM_DATA_PAGE_SPACE_SMALL)
        {
            var result = (extendValue & 0b00000000_100_100_100_100_100_100_100_100) 
                ^ 0b00000000_100_100_100_100_100_100_100_100;

            return result > 0;
        }
        else if (type == PageType.Data && length < AM_DATA_PAGE_SPACE_MEDIUM)
        {
            var notA = (extendValue & 0b00000000_100_100_100_100_100_100_100_100) ^ 0b00000000_100_100_100_100_100_100_100_100;
            var notB = (extendValue & 0b00000000_010_010_010_010_010_010_010_010) ^ 0b00000000_010_010_010_010_010_010_010_010;
            var notC = (extendValue & 0b00000000_001_001_001_001_001_001_001_001) ^ 0b00000000_001_001_001_001_001_001_001_001;
            var b = extendValue & 0b00000000_010_010_010_010_010_010_010_010;

            var result = (notA & notB) | (notA & notC & b);

            return result > 0;

        }

        return false;
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
            (PageType.Data, < AM_DATA_PAGE_SPACE_SMALL) => 4,
            (PageType.Index, >= AM_INDEX_PAGE_SPACE) => 5,
            (PageType.Index, < AM_INDEX_PAGE_SPACE) => 6,
            _ => throw new NotSupportedException()
        };
    }

    #endregion
}
