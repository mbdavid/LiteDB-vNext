namespace LiteDB.Engine;

unsafe internal struct DataBlockResult
{
    public RowID DataBlockID;

    public PageMemory* Page;
    public PageSegment* Segment;
    public DataBlock* DataBlock;

    public static DataBlockResult Empty = new() { DataBlockID = RowID.Empty, Page = default };

    public bool IsEmpty => this.DataBlockID.IsEmpty;

    public int ContentLength => this.Segment->Length - sizeof(DataBlock) - this.DataBlock->Padding;
    public int DocumentLength => this.DataBlock->Extend ? -1 : this.AsSpan().ReadVariantLength(out _);

    public DataBlockResult(PageMemory* page, RowID dataBlockID)
    {
        ENSURE(page->PageID == dataBlockID.PageID);

        this.Page = page;
        this.DataBlockID = dataBlockID;

        this.Reload();
    }

    public void Reload()
    {
        this.Segment = PageMemory.GetSegmentPtr(this.Page, this.DataBlockID.Index);
        this.DataBlock = (DataBlock*)((nint)this.Page + this.Segment->Location);
    }

    /// <summary>
    /// Get DataContent as Span
    /// </summary>
    public Span<byte> AsSpan()
    {
        return new Span<byte>((byte*)((nint)this.Page + this.Segment->Location + sizeof(DataBlock)), this.ContentLength);
    }

}
