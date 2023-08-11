namespace LiteDB.Engine;

internal readonly struct SortItemDocument
{
    public readonly PageAddress RowID;
    public readonly BsonValue Key;
    public readonly BsonDocument Document;

    public SortItemDocument(PageAddress rowID, BsonValue key, BsonDocument document)
    {
        this.RowID = rowID;
        this.Key = key;
        this.Document = document;
    }

    public override string ToString()
    {
        return $"{{ RowID = {RowID}, Key = {Key}, Document = {Document} }}";
    }
}
