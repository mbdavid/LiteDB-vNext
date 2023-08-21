namespace LiteDB.Engine;

internal struct DataBlock
{
    /// <summary>
    /// Get fixed part of DataBlock (5 bytes)
    /// </summary>
    public const int DATA_BLOCK_FIXED_SIZE = 1 + // data block type (reserved)
                                             PageAddress.SIZE; // NextBlock

    public const int P_EXTEND = 0; // 00-00 [byte]
    public const int P_NEXT_BLOCK = 1; // 01-05 [pageAddress]
    public const int P_BUFFER = 6;     // 06-EOF [bytes[]]

    /// <summary>
    /// Data block RowID on DataPage (not persisted)
    /// </summary>
    public readonly PageAddress RowID;

    /// <summary>
    /// Indicate if this data block is first block (false) or extend block (true)
    /// </summary>
    public readonly bool Extend;

    /// <summary>
    /// If document need more than 1 block, use this link to next block
    /// </summary>
    public PageAddress NextBlock;

    /// <summary>
    /// When datablock is first block, read DocumentLength from first 4 bytes. Otherwise, MaxValue (not persisted)
    /// </summary>
    public readonly int DocumentLength;

    /// <summary>
    /// Get how many bytes this data block contains only for document (not persisted)
    /// </summary>
    public readonly int DataLength;

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

        this.Extend = span[P_EXTEND] != 0;
        this.NextBlock = span[P_NEXT_BLOCK..].ReadPageAddress();

        this.DataLength = segment.Length - DATA_BLOCK_FIXED_SIZE;
        this.DocumentLength = this.Extend ? int.MaxValue : span[P_BUFFER..].ReadVariantLength(out _);
    }

    /// <summary>
    /// Create new DataBlock and fill into buffer
    /// </summary>
    public DataBlock(PageBuffer page, PageAddress rowID, bool extend)
    {
        page.IsDirty = true;

        this.RowID = rowID;

        this.Extend = extend;
        this.NextBlock = PageAddress.Empty;

        var segment = PageSegment.GetSegment(page, rowID.Index, out _);
        var span = page.AsSpan(segment);

        span[P_EXTEND] = this.Extend ? (byte)1 : (byte)0;
        span[P_NEXT_BLOCK..].WritePageAddress(PageAddress.Empty);

        this.DataLength = segment.Length - DATA_BLOCK_FIXED_SIZE;
        this.DocumentLength = int.MaxValue;
    }

    /// <summary>
    /// Update NextBlock pointer (update in buffer too)
    /// </summary>
    public void SetNextBlock(PageBuffer page, PageAddress nextBlock)
    {
        ENSURE(this, x => x.RowID.PageID == page.Header.PageID, $"should be same data page {page}");

        page.IsDirty = true;

        this.NextBlock = nextBlock;

        var segment = PageSegment.GetSegment(page, this.RowID.Index, out _);
        var span = page.AsSpan(segment);

        span[P_NEXT_BLOCK..].WritePageAddress(nextBlock);
    }

    /// <summary>
    /// Get span from data content inside dataBlock. Return dataLength as output parameter
    /// </summary>
    public Span<byte> GetDataSpan(PageBuffer page)
    {
        var segment = PageSegment.GetSegment(page, this.RowID.Index, out _);

        return page.AsSpan(segment.Location + DataBlock.P_BUFFER, this.DataLength);
    }

    public override string ToString()
    {
        return $"{{ RowID = {RowID}, Next = {NextBlock} }}";
    }
}
