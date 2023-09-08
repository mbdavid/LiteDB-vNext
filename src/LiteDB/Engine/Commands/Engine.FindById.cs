namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<BsonDocument?> FindById(string collectionName, BsonValue id, string[] fields)
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

        await transaction.InitializeAsync();

        // create both data/index services for this transaction
        var dataService = _factory.CreateDataService(transaction);
        var indexService = _factory.CreateIndexService(transaction);

        // find indexNode based on PK index
        var node = indexService.Find(collection.PK, id, false, LiteDB.Engine.Query.Ascending);

        if (node.IsEmpty) return null;

        // read full document based on first dataBlockID
        var result = dataService.ReadDocument(node.DataBlockID, fields);

        // rollback transaction to release pages back to cache
        transaction.Abort();

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        if (result.Fail) throw result.Exception;

        return result.Value.AsDocument;
    }
}