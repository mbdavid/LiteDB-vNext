namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<int> UpdateAsync(string collectionName, BsonDocument[] documents)
    {
        if (_factory.State != EngineState.Open) throw new Exception("must be open");

        // dependency injection
        var autoIdService = _factory.AutoIdService;
        var masterService = _factory.MasterService;
        var monitorService = _factory.MonitorService;
        var collation = _factory.FileHeader.Collation;

        // get current $master
        var master = masterService.GetMaster(false);

        // if collection do not exists, returns 0
        if (!master.Collections.TryGetValue(collectionName, out var collection)) return 0;

        // create a new transaction locking colID
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { collection.ColID });

        var counter = 0;
        var dataService = _factory.CreateDataService(transaction);
        var indexService = _factory.CreateIndexService(transaction);

        for (var i = 0; i < documents.Length; i++)
        {
            var doc = documents[i];

            var key = doc["_id"];

            if (key.IsNull || key.IsMinValue || key.IsMaxValue) throw ERR("Invalid _id");

            var result = indexService.Find(collection.PK, key, false, LiteDB.Engine.Query.Ascending);

            if (result.IsEmpty) continue;

            // update document content
            dataService.UpdateDocument(result.IndexNodeID, doc);



            //if (collection.Indexes.Count > 1)
            //{
            //    foreach (var index in collection.Indexes.Values)
            //    {
            //        if (index.Name == "_id") continue; // avoid use in linq expression
            //
            //        // get a single or multiple (distinct) values
            //        var keys = index.Expression.GetIndexKeys(doc, collation);
            //
            //        foreach (var key in keys)
            //        {
            //            var node = await indexService.AddNodeAsync(collection.ColID, index, key, -, last);
            //
            //            last = node;
            //        }
            //    }
            //}

            // do a safepoint after insert each document
            if (monitorService.Safepoint(transaction))
            {
                await transaction.SafepointAsync();
            }
        }

        // write all dirty pages into disk
        await transaction.CommitAsync();

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        return counter;
    }
}
