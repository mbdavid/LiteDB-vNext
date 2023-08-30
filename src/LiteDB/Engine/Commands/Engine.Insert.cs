using System;

namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<int> InsertAsync(string collectionName, IEnumerable<BsonDocument> documents, BsonAutoId autoId)
    {
        if (_factory.State != EngineState.Open) throw new Exception("must be open");

        // dependency injection
        var autoIdService = _factory.AutoIdService;
        var masterService = _factory.MasterService;
        var monitorService = _factory.MonitorService;
        var collation = _factory.FileHeader.Collation;

        // get current $master
        var master = masterService.GetMaster(false);

        // if collection do not exists, create in another transaction
        if (!master.Collections.TryGetValue(collectionName, out var collection))
        {
            // create new collection
            await this.CreateCollectionAsync(collectionName);

            // reload $master
            master = masterService.GetMaster(false);

            // get new created collection
            collection = master.Collections[collectionName];
        }

        // create a new transaction locking colID
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { collection.ColID });

        var dataService = _factory.CreateDataService(transaction);
        var indexService = _factory.CreateIndexService(transaction);

        // initialize autoId if needed
        if (autoIdService.NeedInitialize(collection.ColID, autoId))
        {
            autoIdService.Initialize(collection.ColID, collection.PK.TailIndexNodeID, indexService);
        }

        //for (var i = 0; i < documents.Length; i++)
        foreach(var doc in documents)
        {
        //    var doc = documents[i];

            // get/set _id
            var id = autoIdService.SetDocumentID(collection.ColID, doc, autoId);

            // insert document and get position address
            var dataBlockID = dataService.InsertDocument(collection.ColID, doc);

            // insert _id as PK and get node to be used 
            var last = indexService.AddNode(collection.ColID, collection.PK, id, dataBlockID, IndexNodeResult.Empty);

            if (collection.Indexes.Count > 1)
            {
                foreach (var index in collection.Indexes.Values)
                {
                    if (index.Name == "_id") continue; // avoid use in linq expression

                    // get a single or multiple (distinct) values
                    var keys = index.Expression.GetIndexKeys(doc, collation);

                    foreach (var key in keys)
                    {
                        var node = indexService.AddNode(collection.ColID, index, key, dataBlockID, last);

                        last = node;
                    }
                }
            }

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

        // TODO: retornar em formato de array? quem sabe a entrada pode ser um BsonValue (array/document) e o retorno o mesmo
        return 1;
    }
}
