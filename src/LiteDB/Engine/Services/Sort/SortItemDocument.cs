namespace LiteDB.Engine;

internal struct SortItemDocument
{
    public PageAddress RowID;
    public BsonValue Key;
    public BsonDocument Document;

    public SortItemDocument(PageAddress rowID, BsonValue key, BsonDocument document)
    {
        this.RowID = rowID;
        this.Key = key;
        this.Document = document;
    }
}
