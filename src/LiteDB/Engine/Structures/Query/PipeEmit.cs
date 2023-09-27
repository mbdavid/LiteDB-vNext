namespace LiteDB.Engine;

/// <summary>
/// Structure to define enumerators emit after pipe (dataBlockID and/or document)
/// </summary>
internal readonly struct PipeEmit
{
    public readonly bool IndexNodeID;
    public readonly bool DataBlockID;
    public readonly bool Document;

    public PipeEmit(bool indexNodeID, bool dataBlockID, bool document)
    {
        this.IndexNodeID = indexNodeID;
        this.DataBlockID = dataBlockID;
        this.Document = document;
    }

    public override string ToString() => Dump.Object(this);
}
