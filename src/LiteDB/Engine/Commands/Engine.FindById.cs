namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<BsonDocument?> FindById(string collectionName, BsonValue id, HashSet<string>? fields = null)
    {
        var monitorService = _factory.MonitorService;
        var masterService = _factory.MasterService;

        if (_factory.State != EngineState.Open) throw ERR("must be closed");

        // get current $master
        var master = masterService.GetMaster(false);

        // if collection do not exists, return null
        if (!master.Collections.TryGetValue(collectionName, out var collection)) return null;

        // create a new transaction with no collection lock
        var transaction = await monitorService.CreateTransactionAsync(Array.Empty<byte>());

        // create both data/index services for this transaction
        var dataService = _factory.CreateDataService(transaction);
        var indexService = _factory.CreateIndexService(transaction);

        // find indexNode based on PK index
        var node = await indexService.FindAsync(collection.PK, id, false, LiteDB.Query.Ascending);

        if (node is null) return null;

        // read full document based on first datablock
        var doc = await dataService.ReadDocumentAsync(node.Value.Node.RowID, fields);

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        return doc;
    }
}