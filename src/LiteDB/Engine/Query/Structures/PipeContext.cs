namespace LiteDB.Engine;

internal struct PipeContext
{
    public readonly IDataService DataService;
    public readonly IIndexService IndexService;

    public PipeContext(IDataService dataService, IIndexService indexService)
    {
        this.DataService = dataService;
        this.IndexService = indexService;
    }
}
