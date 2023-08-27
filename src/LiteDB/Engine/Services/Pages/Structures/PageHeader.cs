namespace LiteDB.Engine;

/// <summary>
/// Represent a custom header for block pages
/// </summary>
internal struct PageHeader
{
    #region Buffer Field Positions

    public const int P_PAGE_ID = 0;  // 00-03 [int]
    public const int P_PAGE_TYPE = 4; // 04-04 [byte]
    public const int P_POSITION_ID = 5; // 05-08 [int]

    public const int P_COL_ID = 9; // 09-09 [byte]
    public const int P_TRANSACTION_ID = 10; // 10-13 [int]
    public const int P_IS_CONFIRMED = 14; // 14-14 [byte]

    // reserved 15-22 (8 bytes)

    public const int P_ITEMS_COUNT = 23; // 23-23 [byte]
    public const int P_USED_BYTES = 24; // 24-25 [ushort]
    public const int P_FRAGMENTED_BYTES = 26; // 26-27 [ushort]
    public const int P_NEXT_FREE_POSITION = 28; // 28-29 [ushort]
    public const int P_HIGHEST_INDEX = 30; // 30-30 [byte]

    public const int P_CRC8 = 31; // 31-31 [byte] - last byte in page header

    #endregion

    /// <summary>
    /// Bytes used in each offset slot (to store segment position (2) + length (2))
    /// </summary>
    public const int SLOT_SIZE = 4;

    #region Fields

    /// <summary>
    /// Represent page number - start in 0 with HeaderPage [4 bytes] (default: MaxValue)
    /// </summary>
    public int PageID = int.MaxValue;

    /// <summary>
    /// Indicate the page type [1 byte] (default Empty - 0)
    /// </summary>
    public PageType PageType = PageType.Empty;

    /// <summary>
    /// Represent position on disk (used in checkpoint defrag position order) (default: MaxValue)
    /// </summary>
    public int PositionID = int.MaxValue;

    /// <summary>
    /// Get/Set collection ID index (default: 0)
    /// </summary>
    public byte ColID = 0;

    /// <summary>
    /// Represent transaction ID that was stored [4 bytes] (default: 0)
    /// </summary>
    public int TransactionID = 0;

    /// <summary>
    /// Used in WAL, define this page is last transaction page and are confirmed on disk [1 byte] (default: false)
    /// </summary>
    public bool IsConfirmed = false;

    /// <summary>
    /// Indicate how many items are used inside this page [1 byte] -> 0-254 (255) (default: 0)
    /// </summary>
    public byte ItemsCount = 0;

    /// <summary>
    /// Get how many bytes are used on content area (exclude header and footer blocks) [2 bytes] (default: 0)
    /// </summary>
    public ushort UsedBytes = 0;

    /// <summary>
    /// Get how many bytes are fragmented inside this page (free blocks inside used blocks) [2 bytes] (default: 0)
    /// </summary>
    public ushort FragmentedBytes = 0;

    /// <summary>
    /// Get next free location on page. Starts with 32 (first byte after header) - There is no fragmentation after this [2 bytes] (default: 32)
    /// </summary>
    public ushort NextFreeLocation = PAGE_HEADER_SIZE;

    /// <summary>
    /// Get last (highest) used index slot - use byte.MaxValue for empty [1 byte] -> 0-254 (255 items) (default: MaxValue)
    /// </summary>
    public byte HighestIndex = byte.MaxValue;

    /// <summary>
    /// Get/Set CRC8 from content (default: 0)
    /// </summary>
    public byte Crc8 = 0;

    #endregion

    #region Properties

    /// <summary>
    /// Get how many free bytes (including fragmented bytes) are in this page (content space) - Will return 0 bytes if page are full (or with max 255 items)
    /// </summary>
    public int FreeBytes => this.ItemsCount == byte.MaxValue ? // if page is full of items (255) - returns 0 bytes empty
        0 :
        PAGE_CONTENT_SIZE - this.UsedBytes - this.FooterSize;

    /// <summary>
    /// Get how many bytes are used in footer page at this moment
    /// ((HighestIndex + 1) * 4 bytes per slot: [2 for position, 2 for length])
    /// </summary>
    public int FooterSize =>
        (this.HighestIndex == byte.MaxValue ?
        0 :  // no items in page
        ((this.HighestIndex + 1) * SLOT_SIZE)); // 4 bytes PER item (2 to position + 2 to length) - need consider HighestIndex used

    /// <summary>
    /// Get current extend page value based on PageType and FreeSpace
    /// </summary>
    public ExtendPageValue ExtendPageValue => AllocationMapPage.GetExtendPageValue(this.PageType, this.FreeBytes);

    #endregion

    #region DEBUG-Only

    /// <summary>
    /// DEBUG Only - Indicate is a empty/new instance
    /// </summary>
    public bool IsCleanInstance =>
        this.PageID == int.MaxValue &&
        this.PageType == PageType.Empty &&
        this.PositionID == int.MaxValue &&
        this.ColID == 0 &&
        this.TransactionID == 0 &&
        this.IsConfirmed == false &&
        this.ItemsCount == 0 &&
        this.UsedBytes == 0 &&
        this.FragmentedBytes == 0 &&
        this.NextFreeLocation == PAGE_HEADER_SIZE &&
        this.HighestIndex == byte.MaxValue &&
        this.Crc8 == 0;

    #endregion

    /// <summary>
    /// Create a empty header (default value)
    /// </summary>
    public PageHeader()
    {
    }

    public void ReadFromPage(PageBuffer page)
    {
        var span = page.AsSpan();

        this.PageID = span[P_PAGE_ID..].ReadInt32();
        this.PageType = span[P_PAGE_TYPE] switch
        {
            (byte)PageType.Empty => PageType.Empty,
            (byte)PageType.AllocationMap => PageType.AllocationMap,
            (byte)PageType.Index => PageType.Index,
            (byte)PageType.Data => PageType.Data,
            _ => PageType.Unknown
        };
        this.PositionID = span[P_POSITION_ID..].ReadInt32();

        this.ColID = span[P_COL_ID];
        this.TransactionID = span[P_TRANSACTION_ID..].ReadInt32();
        this.IsConfirmed = span[P_IS_CONFIRMED] != 0;

        this.ItemsCount = span[P_ITEMS_COUNT];
        this.UsedBytes = span[P_USED_BYTES..].ReadUInt16();
        this.FragmentedBytes = span[P_FRAGMENTED_BYTES..].ReadUInt16();
        this.NextFreeLocation = span[P_NEXT_FREE_POSITION..].ReadUInt16();
        this.HighestIndex = span[P_HIGHEST_INDEX];

        this.Crc8 = span[P_CRC8];
    }

    public void WriteToPage(PageBuffer page)
    {
        var span = page.AsSpan(0, PAGE_HEADER_SIZE);

        span[P_PAGE_ID..].WriteInt32(this.PageID);
        span[P_PAGE_TYPE] = (byte)this.PageType;
        span[P_POSITION_ID..].WriteInt32(this.PositionID);

        span[P_COL_ID] = this.ColID;
        span[P_TRANSACTION_ID..].WriteInt32(this.TransactionID);
        span[P_IS_CONFIRMED] = this.IsConfirmed ? (byte)1 : (byte)0;

        span[P_ITEMS_COUNT] = this.ItemsCount;
        span[P_USED_BYTES..].WriteUInt16(this.UsedBytes);
        span[P_FRAGMENTED_BYTES..].WriteUInt16(this.FragmentedBytes);
        span[P_NEXT_FREE_POSITION..].WriteUInt16(this.NextFreeLocation);
        span[P_HIGHEST_INDEX] = this.HighestIndex;

        span[P_CRC8] = this.Crc8;
    }

    /// <summary>
    /// Store start index used in GetFreeIndex to avoid always run full loop over all indexes
    /// </summary>
    private byte _startIndex = 0;

    /// <summary>
    /// Reset index used in GetFreeIndex (in delete block)
    /// </summary>
    public void ResetStartIndex() => _startIndex = 0;

    /// <summary>
    /// Get a free index slot in this page
    /// </summary>
    public byte GetFreeIndex(PageBuffer page)
    {
        var span = page.AsSpan();

        // check for all slot area to get first empty slot [safe for byte loop]
        for (var index = _startIndex; index <= this.HighestIndex; index++)
        {
            var segmentAddr = PageSegment.GetSegmentAddr(index);
            var location = span[segmentAddr.Location..].ReadUInt16();

            // if location = 0 means this slot are not used
            if (location == 0)
            {
                _startIndex = (byte)(index + 1);

                return index;
            }
        }

        return (byte)(this.HighestIndex + 1);
    }

    /// <summary>
    /// Checks if segment position/length has a valid value (used for DEBUG)
    /// </summary>
    internal bool IsValidSegment(PageSegment segment) => 
        segment.Location >= PAGE_HEADER_SIZE && segment.Location < (PAGE_SIZE - this.FooterSize) &&
        segment.Length > 0 && segment.Length <= (PAGE_SIZE - PAGE_HEADER_SIZE - this.FooterSize);

    public override string ToString()
    {
        return Dump.Object(new { 
            PageID = Dump.PageID(PageID), 
            PositionID = Dump.PageID(PositionID), 
            PageType, ColID, TransactionID, ItemsCount, FreeBytes, IsConfirmed, HighestIndex, FragmentedBytes, Crc8 });
    }
}
