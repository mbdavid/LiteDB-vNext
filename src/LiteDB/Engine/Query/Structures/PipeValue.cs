namespace LiteDB.Engine;

internal struct PipeValue
{
    public static readonly PipeValue Empty = new();

    public readonly PageAddress IndexNodeID;
    public readonly PageAddress DataBlockID;
    public readonly BsonDocument? Document;

    public readonly bool IsEmpty => this.IndexNodeID.IsEmpty && this.DataBlockID.IsEmpty && this.Document is null;

    public PipeValue(PageAddress indexNodeID, PageAddress dataBlockID)
    {
        this.IndexNodeID = indexNodeID;
        this.DataBlockID = dataBlockID;
        this.Document = null;
    }

    public PipeValue(PageAddress indexNodeID, PageAddress dataBlockID, BsonDocument value)
    {
        this.IndexNodeID = indexNodeID;
        this.DataBlockID = dataBlockID;
        this.Document = value;
    }

    public PipeValue()
    {
        this.IndexNodeID = PageAddress.Empty;
        this.DataBlockID = PageAddress.Empty;
        this.Document = null;
    }

    public override string ToString() => Dump.Object(this);
}
