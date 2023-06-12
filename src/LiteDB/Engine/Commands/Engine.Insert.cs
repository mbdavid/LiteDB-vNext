namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<int> InsertAsync(string collectionName, BsonDocument[] documents, BsonAutoId autoId)
    {
        if (_factory.State != EngineState.Open) throw new Exception("must be open");

        // dependency injection
        var autoIdService = _factory.AutoIdService;
        var masterService = _factory.MasterService;
        var monitorService = _factory.MonitorService;
        var collation = _factory.DiskService.FileHeader.Collation;

        // get current $master
        var master = masterService.GetMaster(false);

        // if collection not exists, create before
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
            await autoIdService.InitializeAsync(collection.ColID, collection.PK.Tail, indexService);
        }

        for (var i = 0; i < documents.Length; i++)
        {
            var doc = documents[i];

            // insert document and get position address
            var rowID = await dataService.InsertDocumentAsync(collection.ColID, doc);

            foreach(var index in collection.Indexes.Values)
            {
                // get a single or multiple (distinct) values
                var keys = index.Expr.GetIndexKeys(doc, collation);

                IndexNodeRef? last = null;

                foreach(var key in keys)
                {
                    var node = await indexService.AddNodeAsync(collection.ColID, index, key, rowID, last);

                    last = node;
                }
            }

            // do a safepoint after insert each document
            monitorService.Safepoint(transaction);
        }

        // write all dirty pages into disk
        await transaction.CommitAsync();

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        // TODO: retornar em formato de array? quem sabe a entrada pode ser um BsonValue (array/document) e o retorno o mesmo
        return 1;
    }
}
