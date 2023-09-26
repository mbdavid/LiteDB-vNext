namespace LiteDB.Engine;

internal class FetchStatement : IFetchStatement
{
    private readonly Guid _cursorID;
    private readonly int _fetchSize;

    public FetchStatement(Guid cursorID, int fetchSize)
    {
        _cursorID = cursorID;
        _fetchSize = fetchSize;
    }

    public async ValueTask<FetchResult> ExecuteFetchAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(31, nameof(ExecuteFetchAsync), nameof(FetchStatement));

        var monitorService = factory.MonitorService;
        var queryService = factory.QueryService;

        if (factory.State != EngineState.Open) throw ERR("must be opened");

        if (!queryService.TryGetCursor(_cursorID, out var cursor))
        {
            throw ERR($"Cursor {_cursorID} do not exists or already full fetched");
        }

        // create a new transaction but use an "old" readVersion
        var transaction = await monitorService.CreateTransactionAsync(cursor.ReadVersion);

        // initialize data/index services for this transaction
        var dataService = factory.CreateDataService(transaction);
        var indexService = factory.CreateIndexService(transaction);

        // create a new context pipe
        var pipeContext = new PipeContext(dataService, indexService, cursor.Parameters);

        // fetch next results (closes cursor when eof)
        var result = queryService.FetchAsync(cursor, _fetchSize, pipeContext);

        // rollback transaction to release pages back to cache
        transaction.Abort();

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        return result;
    }

}
