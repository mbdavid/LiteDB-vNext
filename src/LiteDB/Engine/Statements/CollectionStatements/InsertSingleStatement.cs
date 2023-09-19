namespace LiteDB.Engine;

internal class InsertSingleStatement : IEngineStatement
{
    private readonly IDocumentStore _store;
    private readonly BsonDocument _document;
    private readonly BsonAutoId _autoId;

    public InsertSingleStatement(IDocumentStore store, BsonDocument document, BsonAutoId autoId)
    {
        _store = store;
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
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { _store.ColID });

        // get data/index services from store
        var (dataService, indexService) = _store.GetServices(factory, transaction);

        // get all indexes this store contains
        var indexes = _store.GetIndexes();

        try
        {
            // initialize autoId if needed
            if (autoIdService.NeedInitialize(_store.ColID, _autoId))
            {
                if (indexes.Count > 0)
                {
                    autoIdService.Initialize(_store.ColID, indexes[0].TailIndexNodeID, indexService);
                }
                else
                {
                    autoIdService.Initialize(_store.ColID);
                }
            }

            // insert document and all indexes for this document (based on collection indexes)
            InsertInternal(
                _store.ColID,
                _document,
                _autoId,
                indexes,
                dataService,
                indexService,
                autoIdService,
                collation);

            // write all dirty pages into disk
            await transaction.CommitAsync();

            monitorService.ReleaseTransaction(transaction);

        }
        catch (Exception ex)
        {
            transaction.Abort();

            monitorService.ReleaseTransaction(transaction);

            ErrorHandler.Handle(ex, factory, true);
        }

        return 1;
    }

    /// <summary>
    /// A static function to insert a document and all indexes using only interface services. Will be use in InsertSingle, InsertMany, InsertBulk
    /// </summary>
    public static void InsertInternal(
        byte colID,
        BsonDocument doc, 
        BsonAutoId autoId,
        IReadOnlyList<IndexDocument> indexes,
        IDataService dataService,
        IIndexService indexService,
        IAutoIdService autoIdService, 
        Collation collation)
    {
        using var _pc = PERF_COUNTER(10, nameof(InsertInternal), nameof(InsertSingleStatement));

        // get/set _id
        var id = autoIdService.SetDocumentID(colID, doc, autoId);

        // insert document and get position address
        var dataBlockID = dataService.InsertDocument(colID, doc);

        // insert _id as PK and get node to be used 
        //**var last = indexService.AddNode(collection.ColID, collection.PK, id, dataBlockID, IndexNodeResult.Empty, out _);

        if (indexes.Count > 1)
        {
            for (var i = 1; i < indexes.Count; i++)
            {
                var index = indexes[i];

                // get a single or multiple (distinct) values
                var keys = index.Expression.GetIndexKeys(doc, collation);

                foreach (var key in keys)
                {
                    //**var node = indexService.AddNode(collection.ColID, index, key, dataBlockID, last, out _);

                    //**last = node;
                }
            }
        }

    }
}
