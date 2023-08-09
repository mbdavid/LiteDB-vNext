namespace LiteDB.Engine;

internal class DataServiceLookup : IDocumentLookup
{
    private readonly string[] _fields;

    public DataServiceLookup(string[] fields)
    {
        _fields = fields;
    }

    public async ValueTask<BsonDocument> LoadAsync(PipeValue key, PipeContext context)
    {
        var result = await context.DataService.ReadDocumentAsync(key.RowID, _fields);

        if (result.Fail) throw result.Exception;

        return result.Value.AsDocument;
    }
}