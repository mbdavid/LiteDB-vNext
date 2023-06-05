namespace LiteDB.Engine;

internal class CollectionDocument
{
    public byte ColID { get; }
    public string Name { get; }
    public Dictionary<string, IndexDocument> Indexes { get; } = new (StringComparer.OrdinalIgnoreCase);
    public BsonDocument Meta { get; }

    public CollectionDocument(string name, BsonDocument doc)
    {
        this.Name = name;
        this.ColID = (byte)doc[MK_COL_ID].AsInt32;
        this.Meta = doc[MK_META].AsDocument;

        // get indexes as a document (each key is one slot)
        var indexes = doc[MK_INDEX].AsDocument;

        foreach (var indexName in indexes.Keys)
        {
            var indexDoc = indexes[indexName].AsDocument;
            var index = new IndexDocument(indexName, indexDoc);

            this.Indexes[index.Name] = index;
        }
    }
}

