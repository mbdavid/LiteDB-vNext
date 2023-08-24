namespace LiteDB.Engine;

/// <summary>
/// Structure to define enumerators emit after pipe (dataBlockID and/or document)
/// </summary>
internal struct PipeEmit
{
    public readonly bool DataBlockID;
    public readonly bool Document;

    public PipeEmit(bool dataBlockID, bool value)
    {
        this.DataBlockID = dataBlockID;
        this.Document = value;
    }

    public override string ToString()
    {
        return $"{{ DataBlockID = {DataBlockID}, Document = {Document} }}";
    }
}
