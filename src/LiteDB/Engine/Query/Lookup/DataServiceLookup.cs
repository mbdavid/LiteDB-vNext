namespace LiteDB.Engine;

internal class DataServiceLookup : IDocumentLookup
{
    private readonly HashSet<string> _fields;

    public DataServiceLookup(HashSet<string> fields)
    {
        _fields = fields;
    }

    public ValueTask<BsonDocument> LoadAsync(IndexNode indexNode, IDataService dataService)
    {
        throw new NotSupportedException();
    }

    public async ValueTask<BsonDocument> LoadAsync(PageAddress dataBlock, IDataService dataService)
    {
        var doc = await dataService.ReadDocumentAsync(dataBlock, _fields);

        return doc;
    }
}