namespace LiteDB.Engine;

internal class IndexLookup : IDocumentLookup
{
    private readonly string _field;

    public IndexLookup(string field)
    {
        _field = field;
    }

    public BsonDocument Load(PipeValue key, PipeContext context)
    {
        var doc = new BsonDocument { [_field] = key.Document! };

        return doc;
    }
}