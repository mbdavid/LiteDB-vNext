namespace LiteDB.Engine;

unsafe internal struct DataBlockResult
{
    public RowID DataBlockID;
    public PageMemory* Page;
    public PageSegment* Segment;
    public DataBlock* DataBlock;

    public static DataBlockResult Empty = new() { DataBlockID = RowID.Empty };

    public bool IsEmpty => this.DataBlockID.IsEmpty;

    public int ContentLength => this.Segment->Length - sizeof(DataBlock);
    public int DocumentLength => throw new NotImplementedException();

    public DataBlockResult(RowID dataBlockID, PageMemory* page, PageSegment* segment, DataBlock* dataBlock)
    {
        this.DataBlockID = dataBlockID;
        this.Page = page;
        this.Segment = segment;
        this.DataBlock = dataBlock;
    }

    /// <summary>
    /// Get DataContent as Span
    /// </summary>
    public Span<byte> AsSpan()
    {
        return new Span<byte>((byte*)((nint)this.Page + this.Segment->Location + sizeof(DataBlock)), this.ContentLength);
    }

}
