namespace LiteDB.Engine;

/// <summary>
/// Represent a custom header for block pages
/// </summary>
internal class BlockPageHeader
{
    /// <summary>
    /// Bytes used in each offset slot (to store segment position (2) + length (2))
    /// </summary>
    public const int SLOT_SIZE = 4;

    #region Buffer Field Positions

    public const int P_ITEMS_COUNT = 23; // 23-23 [byte]
    public const int P_USED_BYTES = 24; // 24-25 [ushort]
    public const int P_FRAGMENTED_BYTES = 26; // 26-27 [ushort]
    public const int P_NEXT_FREE_POSITION = 28; // 28-29 [ushort]
    public const int P_HIGHEST_INDEX = 30; // 30-30 [byte]

    #endregion

    /// <summary>
    /// Indicate how many items are used inside this page [1 byte]
    /// </summary>
    public byte ItemsCount { get; set; } = 0;

    /// <summary>
    /// Get how many bytes are used on content area (exclude header and footer blocks) [2 bytes]
    /// </summary>
    public ushort UsedBytes { get; set; } = 0;

    /// <summary>
    /// Get how many bytes are fragmented inside this page (free blocks inside used blocks) [2 bytes]
    /// </summary>
    public ushort FragmentedBytes { get; set; } = 0;

    /// <summary>
    /// Get next free position. Starts with 32 (first byte after header) - There is no fragmentation after this [2 bytes]
    /// </summary>
    public ushort NextFreePosition { get; set; } = PAGE_HEADER_SIZE;

    /// <summary>
    /// Get last (highest) used index slot - use byte.MaxValue for empty [1 byte]
    /// </summary>
    public byte HighestIndex { get; set; } = byte.MaxValue;

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

    /// <summary>
    /// Create a empty header (default value)
    /// </summary>
    public BlockPageHeader()
    {
    }

    /// <summary>
    /// Create instance based on buffer data
    /// </summary>
    public BlockPageHeader(Span<byte> span)
    {
        this.ItemsCount = span[P_ITEMS_COUNT];
        this.UsedBytes = span[P_USED_BYTES..2].ReadUInt16();
        this.FragmentedBytes = span[P_FRAGMENTED_BYTES..2].ReadUInt16();
        this.NextFreePosition = span[P_NEXT_FREE_POSITION..2].ReadUInt16();
        this.HighestIndex = span[P_HIGHEST_INDEX];
    }

    /// <summary>
    /// Create new instance based on another instance (copy)
    /// </summary>
    public BlockPageHeader(BlockPageHeader source)
    {
        this.ItemsCount = source.ItemsCount;
        this.UsedBytes = source.UsedBytes;
        this.FragmentedBytes = source.FragmentedBytes;
        this.NextFreePosition = source.NextFreePosition;
        this.HighestIndex = source.HighestIndex;
    }

    public void Update(Span<byte> span)
    {
        span[P_ITEMS_COUNT] = this.ItemsCount;
        span[P_USED_BYTES..2].WriteUInt16(this.UsedBytes);
        span[P_FRAGMENTED_BYTES..2].WriteUInt16(this.FragmentedBytes);
        span[P_NEXT_FREE_POSITION..2].WriteUInt16(this.NextFreePosition);
        span[P_HIGHEST_INDEX] = this.HighestIndex;
    }

    /// <summary>
    /// Store start index used in GetFreeIndex to avoid always run full loop over all indexes
    /// </summary>
    private byte _startIndex = 0;

    /// <summary>
    /// Get a free index slot in this page
    /// </summary>
    public byte GetFreeIndex(Span<byte> span)
    {
        // check for all slot area to get first empty slot [safe for byte loop]
        for (byte index = _startIndex; index <= this.HighestIndex; index++)
        {
            var positionAddr = BlockPage.CalcPositionAddr(index);
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
