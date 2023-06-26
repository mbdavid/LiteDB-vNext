using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

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

        // create a new transaction with no lock
        var transaction = await monitorService.CreateTransactionAsync(cursor.ReadVersion);

        var dataService = _factory.CreateDataService(transaction);
        var indexService = _factory.CreateIndexService(transaction);

        var result = await queryService.FetchAsync(cursor, dataService, indexService, fetchSize);

        monitorService.ReleaseTransaction(transaction);

        return result;
    }
}