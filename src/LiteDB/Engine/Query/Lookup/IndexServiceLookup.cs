namespace LiteDB.Engine;

internal class IndexServiceLookup : IDocumentLookup
{
    private readonly string _field;

    public IndexServiceLookup(string field)
    {
        _field = field;
    }

    public ValueTask<BsonDocument> LoadAsync(PipeValue key, PipeContext context)
    {
        var doc = new BsonDocument { [_field] = key.Document! };

        return new ValueTask<BsonDocument>(doc);
    }
}