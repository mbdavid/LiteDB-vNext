namespace LiteDB.Engine;

internal class CollectionDocument
{
    public byte ColID { get; }
    public string Name { get; }
    public IDictionary<string, IndexDocument> Indexes { get; }
    public BsonDocument Meta { get; }

    public CollectionDocument(byte colID, string name, PageAddress head, PageAddress tail)
    {
        this.ColID = colID;
        this.Name = name;
        this.Meta = new BsonDocument();

        // create primary key index
        this.Indexes = new Dictionary<string, IndexDocument>(StringComparer.OrdinalIgnoreCase)
        {
            ["_id"] = new IndexDocument(head, tail)
        };
    }

    public CollectionDocument(byte colID, BsonDocument doc)
    {
        this.ColID = colID;
        this.Name = doc[MK_COL_NAME];
        this.Meta = doc[MK_META].AsDocument;
        this.Indexes = new Dictionary<string, IndexDocument>(StringComparer.OrdinalIgnoreCase);

        // get indexes as a document (each key is one slot)
        var indexes = doc[MK_INDEX].AsDocument;

        foreach (var key in indexes.Keys)
        {
            var idxDoc = indexes[key].AsDocument;
            var index = new IndexDocument(Convert.ToByte(key), idxDoc);

            this.Indexes[index.Name] = index;
        }
    }
}

