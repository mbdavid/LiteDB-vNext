namespace LiteDB.Engine;

internal struct PipeContext
{
    public readonly I__DataService DataService;
    public readonly I__IndexService IndexService;
    public readonly BsonDocument QueryParameters;

    public PipeContext(I__DataService dataService, I__IndexService indexService, BsonDocument queryParameters)
    {
        this.DataService = dataService;
        this.IndexService = indexService;
        this.QueryParameters = queryParameters;
    }
}
