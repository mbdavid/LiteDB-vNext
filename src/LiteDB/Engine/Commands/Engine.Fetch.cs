using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Sources;

namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<FetchResult> FetchAsync(Guid cursorID, int fetchSize)
    {
        var monitorService = _factory.MonitorService;
        var queryService = _factory.QueryService;

        if (_factory.State != EngineState.Open) throw ERR("must be opened");

        if (!queryService.TryGetCursor(cursorID, out var cursor))
        {
            throw ERR($"Cursor {cursorID} do not exists or already full fetched");
        }

        // create a new transaction but use an "old" readVersion
        var transaction = await monitorService.CreateTransactionAsync(cursor.ReadVersion);

        // read lock transaction
        await transaction.InitializeAsync();

        // initialize data/index services for this transaction
        var dataService = _factory.CreateDataService(transaction);
        var indexService = _factory.CreateIndexService(transaction);

        // create a new context pipe
        var pipeContext = new PipeContext(dataService, indexService, cursor.Parameters);

        // fetch next results (closes cursor when eof)
        var result = await queryService.FetchAsync(cursor, fetchSize, pipeContext);

        // rollback transaction to release pages back to cache
        transaction.Rollback();

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        return result;
    }
}