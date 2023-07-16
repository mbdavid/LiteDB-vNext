namespace LiteDB.Engine;

internal struct PipeValue
{
    public static readonly PipeValue Empty = new();

    public readonly PageAddress RowID;
    public readonly BsonDocument? Document;

    public readonly bool IsEmpty => this.RowID.IsEmpty && this.Document is null;

    public PipeValue(PageAddress rowID)
    {
        this.RowID = rowID;
        this.Document = null;
    }

    public PipeValue(PageAddress rowID, BsonDocument value)
    {
        this.RowID = rowID;
        this.Document = value;
    }

    public PipeValue()
    {
        this.RowID = PageAddress.Empty;
        this.Document = null;
    }

    public override string ToString()
    {
        return this.IsEmpty ? "<EMPTY>" : $"[{this.RowID}] = {this.Document}";
    }
}
