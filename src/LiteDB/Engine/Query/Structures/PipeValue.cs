namespace LiteDB.Engine;

internal struct PipeValue
{
    public static readonly PipeValue Empty = new();

    public readonly PageAddress RowID;
    public readonly BsonDocument? Value;

    public readonly bool IsEmpty => this.RowID.IsEmpty && this.Value is null;

    public PipeValue(PageAddress rowID)
    {
        this.RowID = rowID;
        this.Value = null;
    }

    public PipeValue(PageAddress rowID, BsonDocument value)
    {
        this.RowID = rowID;
        this.Value = value;
    }

    public PipeValue()
    {
        this.RowID = PageAddress.Empty;
        this.Value = null;
    }

    public override string ToString()
    {
        return this.IsEmpty ? "<EMPTY>" : $"[{this.RowID}] = {this.Value}";
    }
}
