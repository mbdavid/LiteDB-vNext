namespace LiteDB.Engine;

internal readonly struct PipeValue : IIsEmpty
{
    public readonly RowID IndexNodeID;
    public readonly RowID DataBlockID;
    public readonly BsonDocument? Document;

    public static readonly PipeValue Empty = new();

    public readonly bool IsEmpty => this.IndexNodeID.IsEmpty && this.DataBlockID.IsEmpty && this.Document is null;

    public PipeValue(RowID indexNodeID, RowID dataBlockID)
    {
        this.IndexNodeID = indexNodeID;
        this.DataBlockID = dataBlockID;
        this.Document = null;
    }

    public PipeValue(RowID indexNodeID, RowID dataBlockID, BsonDocument value)
    {
        this.IndexNodeID = indexNodeID;
        this.DataBlockID = dataBlockID;
        this.Document = value;
    }

    public PipeValue(BsonDocument value)
    {
        this.IndexNodeID = RowID.Empty;
        this.DataBlockID = RowID.Empty;
        this.Document = value;
    }

    public PipeValue()
    {
        this.IndexNodeID = RowID.Empty;
        this.DataBlockID = RowID.Empty;
        this.Document = null;
    }

    public override string ToString() => Dump.Object(this);
}
