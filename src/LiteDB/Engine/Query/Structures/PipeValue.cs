namespace LiteDB.Engine;

internal struct PipeValue
{
    public static readonly PipeValue Empty = new();

    public readonly PageAddress DataBlockID;
    public readonly BsonDocument? Document;

    public readonly bool IsEmpty => this.DataBlockID.IsEmpty && this.Document is null;

    public PipeValue(PageAddress dataBlockID)
    {
        this.DataBlockID = dataBlockID;
        this.Document = null;
    }

    public PipeValue(PageAddress dataBlockID, BsonDocument value)
    {
        this.DataBlockID = dataBlockID;
        this.Document = value;
    }

    public PipeValue()
    {
        this.DataBlockID = PageAddress.Empty;
        this.Document = null;
    }

    public override string ToString()
    {
        return this.IsEmpty ? "<EMPTY>" : $"{{ DataBlockID = {DataBlockID}, Document = {Document} }}";
    }
}
