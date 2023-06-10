namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<int> InsertAsync(string collectionName, BsonDocument document)
    {
        if (_factory.State != EngineState.Open) throw new Exception("must be open");

        // dependency injection
        var masterService = _factory.MasterService;
        var monitorService = _factory.MonitorService;

        // get current $master
        var master = masterService.GetMaster(false);

        if (master.Collections.TryGetValue(collectionName, out var collection)) throw ERR("criar antes");

        // create a new transaction locking colID
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { collection.ColID });

        // get index service
        var indexer = _factory.CreateIndexService(transaction);

        var pk = collection.Indexes["_id"];

        var keys = new BsonValue[] { 12, 17, 20, 25, 31, 38, 39, 44, 50, 55 };

        foreach (var key in keys)
        {
            await indexer.AddNodeAsync(collection.ColID, pk, key, PageAddress.Empty, null);
        }


        var x0 = await indexer.FindAsync(pk, 20, false, Query.Ascending);

        var x1 = await indexer.FindAsync(pk, 22, false, Query.Ascending);

        var x2 = await indexer.FindAsync(pk, 22, true, Query.Descending);


        // write all dirty pages into disk
        await transaction.CommitAsync();

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        return 1;
    }
}
