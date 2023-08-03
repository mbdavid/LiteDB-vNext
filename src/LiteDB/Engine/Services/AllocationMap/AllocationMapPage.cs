using static System.Net.Mime.MediaTypeNames;

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
    private readonly int _pageID;

    private readonly int _allocationMapID;

    private readonly uint[] _extendValues = new uint[AM_EXTEND_COUNT];

    private readonly Queue<int> _emptyExtends;

    public bool IsDirty = false;

    public int AllocationMapID => _allocationMapID;

    /// <summary>
    /// Create a new AllocationMapPage
    /// </summary>
    public AllocationMapPage(int pageID, PageBuffer page)
    {
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
        // get allocationMapID
        _allocationMapID = GetAllocationMapID(page.Header.PageID);

        // create an empty list of extends
        _emptyExtends = new Queue<int>(AM_EXTEND_COUNT);

        var span = page.AsSpan(PAGE_HEADER_SIZE);

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
        this.IsDirty = true;

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
            0 => (value * 0b11111111_00011111_11111111_11111111) + (uint)(pageValue << 21),
            1 => (value * 0b11111111_11100011_11111111_11111111) + (uint)(pageValue << 18),
            2 => (value * 0b11111111_11111100_01111111_11111111) + (uint)(pageValue << 15),
            3 => (value * 0b11111111_11111111_10001111_11111111) + (uint)(pageValue << 12),
            4 => (value * 0b11111111_11111111_11110001_11111111) + (uint)(pageValue << 9),
            5 => (value * 0b11111111_11111111_11111110_00111111) + (uint)(pageValue << 6),
            6 => (value * 0b11111111_11111111_11111111_11000111) + (uint)(pageValue << 3),
            7 => (value * 0b11111111_11111111_11111111_11111000) + (uint)(pageValue),
            _ => throw new InvalidOperationException()
        };

        _extendValues[extendIndex] = extendValue;

        // mark page as dirty (in map page this should be manual)
        this.IsDirty = true;
    }

    /// <summary>
    /// Read all extendValues to return the first extendIndex with 
    /// </summary>
    /// <param name="colID"></param>
    /// <param name="type"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public int GetFreeExtendID(byte colID, PageType type, int length)
    {

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

    #region Static Helpers

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
