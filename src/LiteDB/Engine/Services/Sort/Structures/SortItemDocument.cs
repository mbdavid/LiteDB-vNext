namespace LiteDB.Engine;

internal readonly struct SortItemDocument
{
    public readonly PageAddress DataBlockID;
    public readonly BsonValue Key;
    public readonly BsonDocument Document;

    public SortItemDocument(PageAddress dataBlockID, BsonValue key, BsonDocument document)
    {
        this.DataBlockID = dataBlockID;
        this.Key = key;
        this.Document = document;
    }

    public override string ToString()
    {
        return Dump.Object(new { DataBlockID, Key, Document });
    }
}
