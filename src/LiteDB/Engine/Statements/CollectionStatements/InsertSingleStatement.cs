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

        try
        {
            // initialize autoId if needed
            if (autoIdService.NeedInitialize(_collection.ColID, _autoId))
            {
                autoIdService.Initialize(_collection.ColID, _collection.PK.TailIndexNodeID, indexService);
            }

            InsertInternal(
                _collection,
                _document,
                _autoId,
                autoIdService,
                dataService,
                indexService,
                collation);

            // write all dirty pages into disk
            await transaction.CommitAsync();

        }
        catch (Exception ex)
        {
            transaction.Abort();

        }
        finally
        {
            monitorService.ReleaseTransaction(transaction);
        }


        return 1;
    }

    public static void InsertInternal(
        ISourceStore collection, 
        BsonDocument doc, 
        BsonAutoId autoId, 
        IAutoIdService autoIdService, 
        IDataService dataService, 
        IIndexService indexService,
        Collation collation)
    {
        using var _p2 = PERF_COUNTER(10, "InsertSingle", nameof(LiteEngine));

        // get/set _id
        var id = autoIdService.SetDocumentID(collection.ColID, doc, autoId);

        // insert document and get position address
        var dataBlockID = dataService.InsertDocument(collection.ColID, doc);

        // insert _id as PK and get node to be used 
        var last = indexService.AddNode(collection.ColID, collection.PK, id, dataBlockID, IndexNodeResult.Empty, out _);

        if (collection.Indexes.Count > 1)
        {
            for (var i = 1; i < collection.Indexes.Count; i++)
            {
                var index = collection.Indexes[i];

                // get a single or multiple (distinct) values
                var keys = index.Expression.GetIndexKeys(doc, collation);

                foreach (var key in keys)
                {
                    var node = indexService.AddNode(collection.ColID, index, key, dataBlockID, last, out _);

                    last = node;
                }
            }
        }

    }
}
