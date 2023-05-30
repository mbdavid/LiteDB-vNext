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
    public PageAddress NextBlock;

    /// <summary>
    /// Read new DataBlock from filled page block
    /// </summary>
    public DataBlock(PageBuffer page, PageAddress rowID, PageSegment segment)
    {
        this.RowID = rowID;
        this.Location = location;

        var span = page.AsSpan(location);

        // byte 00-04: NextBlock (PageID, Index)
        this.NextBlock = span[P_NEXT_BLOCK..].ReadPageAddress();
    }

    /// <summary>
    /// Create new DataBlock and fill into buffer
    /// </summary>
    public DataBlock(PageBuffer page, PageAddress rowID, int location, PageAddress nextBlock)
    {
        this.RowID = rowID;
        this.Location = location;
        this.NextBlock = nextBlock;

        var span = page.AsSpan(location);

        // byte 00-04 (can be updated)
        span[P_NEXT_BLOCK..].WritePageAddress(nextBlock);
    }

    public override string ToString()
    {
        return $"RowID: [{this.RowID}] - Next: [{this.NextBlock}]";
    }
}
