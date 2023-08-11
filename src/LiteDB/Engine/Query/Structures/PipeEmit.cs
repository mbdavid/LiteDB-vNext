namespace LiteDB.Engine;

/// <summary>
/// Structure to define enumerators emit after pipe (rowID and/or document)
/// </summary>
internal struct PipeEmit
{
    public readonly bool RowID;
    public readonly bool Document;

    public PipeEmit(bool rowID, bool value)
    {
        this.RowID = rowID;
        this.Document = value;
    }

    public override string ToString()
    {
        return $"{{ RowID = {RowID}, Document = {Document} }}";
    }
}
