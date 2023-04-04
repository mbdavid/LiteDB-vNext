namespace LiteDB.Engine;

internal struct DataBlock
{
    /// <summary>
    /// Get fixed part of DataBlock (5 bytes)
    /// </summary>
    public const int DATA_BLOCK_FIXED_SIZE = PageAddress.SIZE; // NextBlock

    public const int P_NEXT_BLOCK = 0;  // 00-04 [pageAddress]
    public const int P_BUFFER = 5;       // 05-EOF [bytes[]]

    /// <summary>
    /// Block RowID
    /// </summary>
    public readonly PageAddress RowID;

    /// <summary>
    /// If document need more than 1 block, use this link to next block
    /// </summary>
    public readonly PageAddress NextBlock;

    /// <summary>
    /// Read new DataBlock from filled page block
    /// </summary>
    public DataBlock(Span<byte> span, PageAddress rowID)
    {
        this.RowID = rowID;

        // byte 00-04: NextBlock (PageID, Index)
        this.NextBlock = span[P_NEXT_BLOCK..].ReadPageAddress();
    }

    /// <summary>
    /// Create new DataBlock and fill into buffer
    /// </summary>
    public DataBlock(Span<byte> span, PageAddress rowID, PageAddress nextBlock)
    {
        this.RowID = rowID;

        this.NextBlock = nextBlock;

        // byte 00-04 (can be updated)
        span[P_NEXT_BLOCK..].WritePageAddress(nextBlock);
    }

    public override string ToString()
    {
        return $"Pos: [{this.RowID}] - Next: [{this.NextBlock}]";
    }
}
