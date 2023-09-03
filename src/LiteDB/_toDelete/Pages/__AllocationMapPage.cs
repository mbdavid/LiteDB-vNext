namespace LiteDB.Engine;

/// <summary>
/// Represent a single allocation map page with 2040 extends and 16.320 pages pointer
/// Each extend represent 8 pages at same collection. Each extend use 4 bytes (UInt32)
/// 
///  00000000  000_000_000_000_000_000_000_000
///     ColID    0   1   2   3   4   5   6   7 -- PagesIndex
/// </summary>
[Obsolete]
internal class __AllocationMapPage
{
    private readonly int _allocationMapID;

    private readonly PageBuffer _page;

    private readonly uint[] _extendValues = new uint[AM_EXTEND_COUNT];

    public int AllocationMapID => _allocationMapID;

    public PageBuffer Page => _page;

    /// <summary>
    /// Create a new AllocationMapPage
    /// </summary>
    public __AllocationMapPage(int pageID, PageBuffer page)
    {
        // keep pageBuffer instance
        _page = page;

        // update page header
        _page.Header.PageID = pageID;
        _page.Header.PageType = PageType.AllocationMap;

        // get allocationMapID
        _allocationMapID = GetAllocationMapID(pageID);

        page.IsDirty = true;
    }

    /// <summary>
    /// Load AllocationMap from buffer memory
    /// </summary>
    public __AllocationMapPage(PageBuffer page)
    {
        // keep pageBuffer instance
        _page = page;

        // get allocationMapID
        _allocationMapID = GetAllocationMapID(page.Header.PageID);

        var span = _page.AsSpan(PAGE_HEADER_SIZE);

        // read all page
        for(var i = 0; i < AM_EXTEND_COUNT; i++)
        {
            var value = span[(i * AM_BYTES_PER_EXTEND)..].ReadExtendValue();

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
    public (int extendIndex, int pageIndex, bool isNew) GetFreeExtend(int currentExtendIndex, byte colID, PageType type)
    {
        while(currentExtendIndex < AM_EXTEND_COUNT)
        {
            // get extend value as uint
            var extendValue = _extendValues[currentExtendIndex];

            var (pageIndex, isNew) = HasFreeSpaceInExtend(extendValue, colID, type);

            // current extend contains a valid page
            if (pageIndex >= 0)
            {
                return (currentExtendIndex, pageIndex, isNew);
            }

            // test if current extend are not empty (create extend here)
            if (extendValue == 0)
            {
                // update extend value with only colID value in first 1 byte (shift 3 bytes)
                _extendValues[currentExtendIndex] = (uint)(colID << 24);

                return (currentExtendIndex, 0, true);
            }

            // go to next index
            currentExtendIndex++;
        }

        return (-1, 0, false);
    }

    /// <summary>
    /// Update extend value based on extendIndex (0-2039) and pageIndex (0-7)
    /// </summary>
    public void UpdateExtendPageValue(int extendIndex, int pageIndex, ExtendPageValue pageValue)
    {
        ENSURE(extendIndex <= 2039);
        ENSURE(pageIndex <= 7);

        // get extend value from array
        var value = _extendValues[extendIndex];

        // update value (3 bits) according pageIndex
        var extendValue = pageIndex switch
        {
            0 => (value & 0b11111111_000_111_111_111_111_111_111_111) | ((uint)pageValue << 21),
            1 => (value & 0b11111111_111_000_111_111_111_111_111_111) | ((uint)pageValue << 18),
            2 => (value & 0b11111111_111_111_000_111_111_111_111_111) | ((uint)pageValue << 15),
            3 => (value & 0b11111111_111_111_111_000_111_111_111_111) | ((uint)pageValue << 12),
            4 => (value & 0b11111111_111_111_111_111_000_111_111_111) | ((uint)pageValue << 9),
            5 => (value & 0b11111111_111_111_111_111_111_000_111_111) | ((uint)pageValue << 6),
            6 => (value & 0b11111111_111_111_111_111_111_111_000_111) | ((uint)pageValue << 3),
            7 => (value & 0b11111111_111_111_111_111_111_111_111_000) | ((uint)pageValue),
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

    public override string ToString()
    {
        return Dump.Object(new { AllocationMapID, PositionID = Dump.PageID(_page.PositionID), PageID = Dump.PageID(_page.Header.PageID), _page.IsDirty });
    }

    #region Static Helpers

    /// <summary>
    /// Check if extend value contains a page that fit on request (type/length)
    /// Returns pageIndex if found or -1 if this extend has no space. Returns if page is new (empty)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int pageIndex, bool isNew) HasFreeSpaceInExtend(uint extendValue, byte colID, PageType type)
    {
        // extendValue (colID + 8 pages values)

        //  01234567   01234567   01234567   01234567
        // [________] [________] [________] [________]
        //  ColID      00011122   23334445   55666777

        // 000 - empty
        // 001 - data 
        // 010 - index 
        // 011 - reserved
        // 100 - reserved
        // 101 - reserved
        // 110 - reserved
        // 111 - full

        // check for same colID
        if (colID != extendValue >> 24) return (-1, false);

        uint result;

        if (type == PageType.Data)
        {
            // 000 - empty
            // 001 - data

            var notA = (extendValue & 0b00000000_100_100_100_100_100_100_100_100) ^ 0b00000000_100_100_100_100_100_100_100_100;
            var notB = (extendValue & 0b00000000_010_010_010_010_010_010_010_010) ^ 0b00000000_010_010_010_010_010_010_010_010;

            notB <<= 1;

            result = notA & notB;
        }
        else if (type == PageType.Index)
        {
            // 000 - empty
            // 010 - index

            var notA = (extendValue & 0b00000000_100_100_100_100_100_100_100_100) ^ 0b00000000_100_100_100_100_100_100_100_100;
            var notC = (extendValue & 0b00000000_001_001_001_001_001_001_001_001) ^ 0b00000000_001_001_001_001_001_001_001_001;

            notC <<= 2;

            result = notA & notC;
        }
        else
        {
            return (-1, false);
        }

        if (result > 0)
        {
            var pageIndex = result switch
            {
                <= 31 => 7,       // 2^(3+2)-1
                <= 255 => 6,      // 2^(6+2)-1
                <= 2047 => 5,     // 2^(9+2)-1
                <= 16383 => 4,    // 2^(12+2)-1
                <= 131071 => 3,   // 2^(15+2)-1
                <= 1048575 => 2,  // 2^(18+2)-1
                <= 8388607 => 1,  // 2^(21+2)-1
                <= 67108863 => 0, // 2^(24+2)-1
                _ => throw new NotSupportedException()
            };

            var isEmpty = (extendValue & (0b111 << ((7 - pageIndex) * 3))) == 0;

            return (pageIndex, isEmpty);
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
        return (pageID - __AM_FIRST_PAGE_ID) % AM_EXTEND_COUNT;
    }

    /// <summary>
    /// Get a value (0-7) thats represent diferent page types/avaiable spaces
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExtendPageValue GetExtendPageValue(PageType pageType, int freeBytes)
    {
        return (pageType, freeBytes) switch
        {
            (_, PAGE_CONTENT_SIZE) => ExtendPageValue.Empty,
            (PageType.Data, >= AM_DATA_PAGE_FREE_SPACE) => ExtendPageValue.Data,
            (PageType.Index, >= AM_INDEX_PAGE_FREE_SPACE) => ExtendPageValue.Index,
            _ => ExtendPageValue.Full
        };
    }

    #endregion
}
