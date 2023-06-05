namespace LiteDB.Engine;

[AutoInterface]
internal class InsertCommand : IInsertCommand
{
    private readonly IServicesFactory _factory;
    private readonly IDiskService _disk;
    private readonly ILockService _lock;
    private readonly IMasterService _master;
    private readonly IAllocationMapService _allocationMap;
    private ITransactionMonitor _monitor;
    private readonly IEngineContext _ctx;

    public InsertCommand(IServicesFactory factory, IEngineContext ctx)
    {
        _factory = factory;
        _disk = factory.GetDisk();
        _lock = factory.GetLock();
        _master = factory.GetMaster();
        _allocationMap = factory.GetAllocationMap();
        _monitor = factory.GetMonitor();

        _ctx = ctx;
    }

    public async Task ExecuteAsync(string collectionName, BsonDocument document, CancellationToken cancellationToken = default)
    {
        if (_factory.State != EngineState.Open) throw new Exception("must be open");

        if (!_master.Collections!.TryGetValue(collectionName, out var collection)) throw ERR("criar antes");

        // create a new transaction locking colID
        var transaction = await _monitor.CreateTransactionAsync(new byte[] { collection.ColID });

        // get index service
        var indexer = _factory.CreateIndexService(transaction);

        var pk = collection.Indexes["_id"];

        var keys = new BsonValue[] { 12,17,20,25,31,38,39,44,50,55 };

        foreach(var key in keys)
        {
            await indexer.AddNodeAsync(collection.ColID, pk, key, PageAddress.Empty, null);
        }


        var x0 = await indexer.FindAsync(pk, 20, false, Query.Ascending);

        var x1 = await indexer.FindAsync(pk, 22, false, Query.Ascending);

        var x2 = await indexer.FindAsync(pk, 22, true, Query.Descending);


        // write all dirty pages into disk
        await transaction.CommitAsync();

        // release transaction
        _monitor.ReleaseTransaction(transaction);



    }
}
