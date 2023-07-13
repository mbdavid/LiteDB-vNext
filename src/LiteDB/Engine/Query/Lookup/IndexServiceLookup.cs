namespace LiteDB.Engine;

internal class IndexServiceLookup : IDocumentLookup
{
    private readonly string _name;

    public IndexServiceLookup(string name)
    {
        _name = name;
    }

    public ValueTask<BsonDocument> LoadAsync(PipeValue key, PipeContext context)
    {
        var doc = new BsonDocument { [_name] = key.Value! };

        return new ValueTask<BsonDocument>(doc);
    }
}