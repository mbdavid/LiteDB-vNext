namespace LiteDB.Engine;

/// <summary>
/// Represent a custom header for block pages
/// </summary>
internal struct PageHeader
{
    #region Buffer Field Positions

    public const int P_PAGE_ID = 0;  // 00-03 [uint]
    public const int P_PAGE_TYPE = 4; // 04-04 [byte]

    public const int P_COL_ID = 5; // 05-05 [byte]
    public const int P_TRANSACTION_ID = 6; // 06-10 [uint]
    public const int P_IS_CONFIRMED = 11; // 11-11 [byte]

    public const int P_ITEMS_COUNT = 23; // 23-23 [byte]
    public const int P_USED_BYTES = 24; // 24-25 [ushort]
    public const int P_FRAGMENTED_BYTES = 26; // 26-27 [ushort]
    public const int P_NEXT_FREE_POSITION = 28; // 28-29 [ushort]
    public const int P_HIGHEST_INDEX = 30; // 30-30 [byte]

    public const int P_CRC8 = 31; // 1 byte (last byte in header)

    #endregion

    /// <summary>
    /// Bytes used in each offset slot (to store segment position (2) + length (2))
    /// </summary>
    public const int SLOT_SIZE = 4;

    #region Fields

    /// <summary>
    /// Represent page number - start in 0 with HeaderPage [4 bytes]
    /// </summary>
    public uint PageID = uint.MaxValue;

    /// <summary>
    /// Indicate the page type [1 byte]
    /// </summary>
    public PageType PageType = PageType.Empty;

    /// <summary>
    /// Get/Set collection ID index
    /// </summary>
    public byte ColID = 0;

    /// <summary>
    /// Represent transaction ID that was stored [4 bytes]
    /// </summary>
    public uint TransactionID = 0;

    /// <summary>
    /// Used in WAL, define this page is last transaction page and are confirmed on disk [1 byte]
    /// </summary>
    public bool IsConfirmed = false;

    /// <summary>
    /// Indicate how many items are used inside this page [1 byte]
    /// </summary>
    public byte ItemsCount = 0;

    /// <summary>
    /// Get how many bytes are used on content area (exclude header and footer blocks) [2 bytes]
    /// </summary>
    public ushort UsedBytes = 0;

    /// <summary>
    /// Get how many bytes are fragmented inside this page (free blocks inside used blocks) [2 bytes]
    /// </summary>
    public ushort FragmentedBytes = 0;

    /// <summary>
    /// Get next free position. Starts with 32 (first byte after header) - There is no fragmentation after this [2 bytes]
    /// </summary>
    public ushort NextFreePosition = PAGE_HEADER_SIZE;

    /// <summary>
    /// Get last (highest) used index slot - use byte.MaxValue for empty [1 byte]
    /// </summary>
    public byte HighestIndex = byte.MaxValue;

    /// <summary>
    /// Get/Set CRC8 from content
    /// </summary>
    public byte Crc8 = 0;

    #endregion

    #region Properties

    /// <summary>
    /// Get how many free bytes (including fragmented bytes) are in this page (content space) - Will return 0 bytes if page are full (or with max 255 items)
    /// </summary>
    public int FreeBytes => this.ItemsCount == byte.MaxValue ?
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

    #endregion

    /// <summary>
    /// Create a empty header (default value)
    /// </summary>
    public PageHeader()
    {
    }

    public void ReadFromBuffer(Span<byte> span)
    {
        this.PageID = span[P_PAGE_ID..].ReadUInt32();
        this.PageType = (PageType)span[P_PAGE_TYPE];

        this.ColID = span[P_COL_ID];
        this.TransactionID = span[P_TRANSACTION_ID..].ReadUInt32();
        this.IsConfirmed = span[P_IS_CONFIRMED] != 0;

        this.ItemsCount = span[P_ITEMS_COUNT];
        this.UsedBytes = span[P_USED_BYTES..2].ReadUInt16();
        this.FragmentedBytes = span[P_FRAGMENTED_BYTES..2].ReadUInt16();
        this.NextFreePosition = span[P_NEXT_FREE_POSITION..2].ReadUInt16();
        this.HighestIndex = span[P_HIGHEST_INDEX];

        this.Crc8 = span[P_CRC8];
    }

    public void WriteToBuffer(Span<byte> span)
    {
        span[P_PAGE_ID..].WriteUInt32(this.PageID);
        span[P_PAGE_TYPE] = (byte)this.PageType;

        span[P_COL_ID] = this.ColID;
        span[P_TRANSACTION_ID..].WriteUInt32(this.TransactionID);
        span[P_IS_CONFIRMED] = this.IsConfirmed ? (byte)1 : (byte)0;

        span[P_ITEMS_COUNT] = this.ItemsCount;
        span[P_USED_BYTES..2].WriteUInt16(this.UsedBytes);
        span[P_FRAGMENTED_BYTES..2].WriteUInt16(this.FragmentedBytes);
        span[P_NEXT_FREE_POSITION..2].WriteUInt16(this.NextFreePosition);
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
    public byte GetFreeIndex(Span<byte> span)
    {
        // check for all slot area to get first empty slot [safe for byte loop]
        for (byte index = _startIndex; index <= this.HighestIndex; index++)
        {
            var positionAddr = BasePageService.CalcPositionAddr(index);
            var position = span[positionAddr..2].ReadUInt16();

            // if position = 0 means this slot are not used
            if (position == 0)
            {
                _startIndex = (byte)(index + 1);

                return index;
            }
        }

        return (byte)(this.HighestIndex + 1);
    }

}
