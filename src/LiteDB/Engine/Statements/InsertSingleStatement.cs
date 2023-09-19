using System.Collections;

namespace LiteDB.Engine;

internal class InsertSingleStatement : IEngineStatement
{
    private readonly ITargetStore _collection;
    private readonly BsonDocument _document;
    private readonly BsonAutoId _autoId;

    public InsertSingleStatement(ITargetStore collection, BsonDocument document, BsonAutoId autoId)
    {
        _collection = collection;
        _document = document;
        _autoId = autoId;
    }

    public async ValueTask<int> Execute(IServicesFactory factory)
    {
        using var _pc = PERF_COUNTER(31, nameof(InsertSingleStatement), nameof(LiteEngine));

        if (factory.State != EngineState.Open) throw new Exception("must be open");

        // dependency injection
        var autoIdService = factory.AutoIdService;
        var masterService = factory.MasterService;
        var monitorService = factory.MonitorService;
        var collation = factory.FileHeader.Collation;

        // create a new transaction locking colID
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { _collection.ColID });

        var dataService = factory.CreateDataService(transaction);
        var indexService = factory.CreateIndexService(transaction);

        // initialize autoId if needed
        if (autoIdService.NeedInitialize(_collection.ColID, _autoId))
        {
            autoIdService.Initialize(_collection.ColID, _collection.PK.TailIndexNodeID, indexService);
        }

        // getting headerNodeResult (node+page) for all indexes
        var headResults = new IndexNodeResult[collection.Indexes.Count];

        for (var i = 0; i < collection.Indexes.Count; i++)
        {
            var index = collection.Indexes[i];
            headResults[i] = indexService.GetNode(index.HeadIndexNodeID);
        }

        //for (var i = 0; i < documents.Length; i++)
        foreach (var doc in documents)
        {
            using var _p2 = PERF_COUNTER(10, "InsertSingle", nameof(LiteEngine));

            // get/set _id
            var id = autoIdService.SetDocumentID(collection.ColID, doc, autoId);

            // insert document and get position address
            var dataBlockID = dataService.InsertDocument(collection.ColID, doc);

            // insert _id as PK and get node to be used 
            var last = indexService.AddNode(collection.ColID, collection.PK, id, dataBlockID, headResults[0], IndexNodeResult.Empty, out _);

            if (collection.Indexes.Count > 1)
            {
                for (var i = 1; i < collection.Indexes.Count; i++)
                {
                    var index = collection.Indexes[i];

                    // get a single or multiple (distinct) values
                    var keys = index.Expression.GetIndexKeys(doc, collation);

                    foreach (var key in keys)
                    {
                        var node = indexService.AddNode(collection.ColID, index, key, dataBlockID, headResults[i], last, out _);

                        last = node;
                    }
                }
            }

            // do a safepoint after insert each document
            if (monitorService.Safepoint(transaction))
            {
                await transaction.SafepointAsync();

                // after safepoint, reload headResult (can change page)
                for (var i = 0; i < collection.Indexes.Count; i++)
                {
                    var index = collection.Indexes[i];

                    headResults[i] = indexService.GetNode(index.HeadIndexNodeID);
                }
            }
        }

        // write all dirty pages into disk
        await transaction.CommitAsync();

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        // TODO: retornar em formato de array? quem sabe a entrada pode ser um BsonValue (array/document) e o retorno o mesmo
        return 1;
    }
}
