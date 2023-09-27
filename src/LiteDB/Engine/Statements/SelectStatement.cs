namespace LiteDB.Engine;

internal class SelectStatement : IEngineStatement
{
    private readonly Query _query;
    private readonly int _fetchSize;

    public EngineStatementType StatementType => EngineStatementType.Select;

    public SelectStatement(Query query, int fetchSize)
    {
        _query = query;
        _fetchSize = fetchSize;
    }

    public ValueTask<IDataReader> ExecuteReaderAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(31, nameof(ExecuteReaderAsync), nameof(SelectStatement));

        // get dependencies
        var walIndexService = factory.WalIndexService;
        var queryService = factory.QueryService;

        // get next read version without open a new transaction
        var readVersion = walIndexService.GetNextReadVersion();

        // create cursor after query optimizer and create enumerator pipeline
        var cursor = queryService.CreateCursor(_query, parameters, readVersion);

        // create concrete class to reader cursor
        IDataReader reader = factory.CreateDataReader(cursor, _fetchSize, factory);

        return ValueTask.FromResult(reader);
    }

    public ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters) => throw new NotSupportedException();
}
