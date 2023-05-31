namespace LiteDB.Engine;

[AutoInterface]
internal class CreateCollectionCommand : ICreateCollectionCommand
{
    private readonly IServicesFactory _factory;
    private readonly IDiskService _disk;
    private readonly ILockService _lock;
    private readonly IMasterService _master;
    private readonly IAllocationMapService _allocationMap;
    private readonly ITransactionMonitor _monitor;
    private readonly IEngineContext _ctx;

    public CreateCollectionCommand(IServicesFactory factory, IEngineContext ctx)
    {
        _factory = factory;
        _disk = factory.GetDisk();
        _lock = factory.GetLock();
        _master = factory.GetMaster();
        _allocationMap = factory.GetAllocationMap();
        _monitor = factory.GetMonitor();

        _ctx = ctx;
    }

    public async Task ExecuteAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        if (_factory.State != EngineState.Open) throw ERR("must be open");

        // create a new transaction locking colID = 255 ($master)
        var transaction = await _monitor.CreateTransactionAsync(new byte[] { MASTER_COL_ID });

        // get a new colID
        var colID = _master.NewColID();

        // get index service
        var indexer = _factory.CreateIndexService(transaction);

        // insert head/tail nodes
        var pkNodes = await indexer.CreateHeadTailNodesAsync(colID);

        // create new collection in $master and returns a new master document
        var master = _master.AddCollection(colID, collectionName, pkNodes.head.RowID, pkNodes.tail.RowID);

        // write master collection into first 8 pages 
        await _master.WriteCollectionAsync(master, transaction);

        // write all dirty pages into disk
        await transaction.CommitAsync();

        // release transaction
        _monitor.ReleaseTransaction(transaction);


    }
}
