namespace LiteDB.Engine;

internal struct PipeValue
{
    public static readonly PipeValue Empty = new();

    public readonly PageAddress RowID;
    public readonly BsonDocument? Value;
    public readonly bool Eof;

    public PipeValue(PageAddress rowID)
    {
        this.RowID = rowID;
        this.Value = null;
        this.Eof = false;
    }

    public PipeValue(PageAddress rowID, BsonDocument value)
    {
        this.RowID = rowID;
        this.Value = value;
        this.Eof = false;
    }

    public PipeValue()
    {
        this.RowID = PageAddress.Empty;
        this.Value = null;
        this.Eof = true;
    }
}
