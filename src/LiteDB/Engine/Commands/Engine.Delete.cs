namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<int> DeleteAsync(string collectionName, BsonValue[] ids)
    {
        if (_factory.State != EngineState.Open) throw new Exception("must be open");

        // dependency injection
        var masterService = _factory.MasterService;
        var monitorService = _factory.MonitorService;

        // get current $master
        var master = masterService.GetMaster(false);

        // if collection do not exists, return 0
        if (!master.Collections.TryGetValue(collectionName, out var collection))
        {
            return 0;
        }

        // create a new transaction locking colID
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { collection.ColID });

        var count = 0;
        var dataService = _factory.CreateDataService(transaction);
        var indexService = _factory.CreateIndexService(transaction);

        for (var i = 0; i < ids.Length; i++)
        {
            var id = ids[i];

            // there is no PK with this values
            if (id.IsNull || id.IsMinValue || id.IsMaxValue) continue;

            var result = indexService.Find(collection.PK, id, false, LiteDB.Engine.Query.Ascending);

            if (result.IsEmpty) continue;

            // delete all index nodes starting from PK
            indexService.DeleteAll(result);

            // delete document
            dataService.DeleteDocument(result.Node.DataBlockID);

            count++;

            // do a safepoint after insert each document
            if (monitorService.Safepoint(transaction))
            {
                transaction.Safepoint();
            }
        }

        // write all dirty pages into disk
        transaction.Commit();

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        return count;
    }
}
