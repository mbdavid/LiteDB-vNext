namespace LiteDB.Engine;

internal struct DataBlock
{
    /// <summary>
    /// Get fixed part of DataBlock (5 bytes)
    /// </summary>
    public const int DATA_BLOCK_FIXED_SIZE = 1 + // data block type (reserved)
                                             PageAddress.SIZE; // NextBlock

    public const int P_BLOCK_TYPE = 0; // 00-00 [byte]
    public const int P_NEXT_BLOCK = 1; // 01-05 [pageAddress]
    public const int P_BUFFER = 6;     // 06-EOF [bytes[]]

    /// <summary>
    /// Data block RowID on DataPage (not persisted)
    /// </summary>
    public readonly PageAddress RowID;

    /// <summary>
    /// Define block type (reserved for future use)
    /// </summary>
    public readonly byte BlockType;

    /// <summary>
    /// If document need more than 1 block, use this link to next block
    /// </summary>
    public readonly PageAddress NextBlock;

    /// <summary>
    /// Read new DataBlock from filled page block
    /// </summary>
    public DataBlock(PageBuffer page, PageAddress rowID)
    {
        this.RowID = rowID;

        ENSURE(() => page.Header.PageID == rowID.PageID, $"PageID {page.Header.PageID} and RowID.PageID {rowID.PageID} must be the same value");
        ENSURE(() => page.Header.PageType == PageType.Data);

        var segment = PageSegment.GetSegment(page, rowID.Index, out _);
        var span = page.AsSpan(segment);

        this.BlockType = span[P_BLOCK_TYPE];
        this.NextBlock = span[P_NEXT_BLOCK..].ReadPageAddress();
    }

    /// <summary>
    /// Create new DataBlock and fill into buffer
    /// </summary>
    public DataBlock(PageBuffer page, PageAddress rowID, PageAddress nextBlock)
    {
        page.IsDirty = true;

        this.RowID = rowID;

        this.BlockType = 1; // reserved
        this.NextBlock = nextBlock;

        var segment = PageSegment.GetSegment(page, rowID.Index, out _);
        var span = page.AsSpan(segment);

        span[P_BLOCK_TYPE] = this.BlockType;
        span[P_NEXT_BLOCK..].WritePageAddress(nextBlock);
    }

    public override string ToString()
    {
        return $"{{ RowID = {RowID}, Next = {NextBlock} }}";
    }
}
