namespace LiteDB.Engine;

internal struct PipeContext
{
    public readonly I__DataService DataService;
    public readonly IIndexService IndexService;
    public readonly BsonDocument QueryParameters;

    public PipeContext(I__DataService dataService, IIndexService indexService, BsonDocument queryParameters)
    {
        this.DataService = dataService;
        this.IndexService = indexService;
        this.QueryParameters = queryParameters;
    }
}
