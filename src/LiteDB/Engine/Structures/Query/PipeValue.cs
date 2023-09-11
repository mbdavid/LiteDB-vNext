namespace LiteDB.Engine;

internal struct PipeValue
{
    public readonly RowID DataBlockID;
    public readonly BsonDocument? Document;

    public static readonly PipeValue Empty = new();

    public readonly bool IsEmpty => this.DataBlockID.IsEmpty && this.Document is null;

    public PipeValue(RowID dataBlockID)
    {
        this.DataBlockID = dataBlockID;
        this.Document = null;
    }

    public PipeValue(RowID dataBlockID, BsonDocument value)
    {
        this.DataBlockID = dataBlockID;
        this.Document = value;
    }

    public PipeValue()
    {
        this.DataBlockID = RowID.Empty;
        this.Document = null;
    }

    public override string ToString() => Dump.Object(this);
}
