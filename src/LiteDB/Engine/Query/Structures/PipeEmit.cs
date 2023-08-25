namespace LiteDB.Engine;

/// <summary>
/// Structure to define enumerators emit after pipe (dataBlockID and/or document)
/// </summary>
internal struct PipeEmit
{
    public readonly bool IndexNodeID;
    public readonly bool DataBlockID;
    public readonly bool Document;

    public PipeEmit(bool indexNodeID, bool dataBlockID, bool value)
    {
        this.IndexNodeID = indexNodeID;
        this.DataBlockID = dataBlockID;
        this.Document = value;
    }

    public override string ToString()
    {
        return $"{{ IndexNodeID = {IndexNodeID}, DataBlockID = {DataBlockID}, Document = {Document} }}";
    }
}
