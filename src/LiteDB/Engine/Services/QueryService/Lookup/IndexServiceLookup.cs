namespace LiteDB.Engine;

internal class IndexServiceLookup : IDocumentLookup
{
    private readonly string _field;

    public IndexServiceLookup(string field)
    {
        _field = field;
    }

    public BsonDocument Load(PipeValue key, PipeContext context)
    {
        var doc = new BsonDocument { [_field] = key.Document! };

        return doc;
    }
}