namespace LiteDB.Engine;

internal struct DataBlock
{
    /// <summary>
    /// Get fixed part of DataBlock (6 bytes)
    /// </summary>
    public const int DATA_BLOCK_FIXED_SIZE = 1 + // DataIndex
                               PageAddress.SIZE; // NextBlock

    private const int P_EXTEND = 0;      // 00-00 [byte]
    private const int P_NEXT_BLOCK = 1;  // 01-05 [pageAddress]
    public const int P_BUFFER = 6;       // 06-EOF [bytes[]]

    /// <summary>
    /// Get position inside page (start..length)
    /// </summary>
    public BlockLocation Location { get; set; }

    /// <summary>
    /// Block RowID
    /// </summary>
    public PageAddress RowID { get; }

    /// <summary>
    /// Indicate if this data block is first block (false) or extend block (true)
    /// </summary>
    public bool Extend { get; }

    /// <summary>
    /// If document need more than 1 block, use this link to next block
    /// </summary>
    public PageAddress NextBlock { get; private set; }

    /// <summary>
    /// Read new DataBlock from filled page block
    /// </summary>
    public DataBlock(Span<byte> span, PageAddress rowID, BlockLocation location)
    {
        this.RowID = rowID;
        this.Location = location;

        // byte 00: Extend
        this.Extend = span[P_EXTEND] != 0;

        // byte 01-05: NextBlock (PageID, Index)
        this.NextBlock = span[P_NEXT_BLOCK..].ReadPageAddress();
    }

    /// <summary>
    /// Create new DataBlock and fill into buffer
    /// </summary>
    public DataBlock(Span<byte> span, PageAddress rowID, BlockLocation location, bool extend, PageAddress nextBlock)
    {
        this.RowID = rowID;
        this.Location = location;

        this.NextBlock = nextBlock;
        this.Extend = extend;

        // byte 00: Data Index
        span[P_EXTEND] = extend ? (byte)1 : (byte)0;

        // byte 01-05 (can be updated)
        span[P_NEXT_BLOCK..].WritePageAddress(nextBlock);
    }

    public void UpdateNextBlock(PageAddress nextBlock, Span<byte> span)
    {
        this.NextBlock = nextBlock;

        span[P_NEXT_BLOCK..].WritePageAddress(nextBlock);
    }

    public override string ToString()
    {
        return $"Pos: [{this.RowID}] - Ext: [{this.Extend}] - Next: [{this.NextBlock}]";
    }
}
