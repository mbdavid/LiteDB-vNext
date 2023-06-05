namespace LiteDB;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface]
internal partial class ServicesFactory : IServicesFactory
{
    private readonly IBsonReader _bsonReader;
    private readonly IBsonWriter _bsonWriter;
    private readonly IBufferFactory _bufferFactory;
    private readonly IMemoryCacheService _memoryCache;

    private readonly ILogService _logService;
    private readonly IWalIndexService _walIndex;

    private readonly ILockService _lock;
    private readonly IStreamFactory _streamFactory;

    private readonly IDiskService _disk;
    private readonly IAllocationMapService _allocationMap;
    private readonly IMasterService _master;
    private readonly ITransactionMonitor _monitor;

    private readonly IDataPageService _dataPageService;
    private readonly IIndexPageService _indexPageService;

    public ServicesFactory(IEngineSettings settings)
    {
        this.Settings = settings;

        // initialize per-engine classes instances (no actions!)

        // no dependencies
        _bsonReader = new BsonReader();
        _bsonWriter = new BsonWriter();
        _memoryCache = new MemoryCacheService();
        _walIndex = new WalIndexService();
        _bufferFactory = new BufferFactory();
        _dataPageService = new DataPageService();
        _indexPageService = new IndexPageService();

        // settings dependency only
        _lock = new LockService(settings.Timeout);
        _streamFactory = new FileStreamFactory(settings);

        _logService = new LogService(_memoryCache, _bufferFactory, _walIndex);

        // other services dependencies
        _disk = new DiskService(settings, _bufferFactory, _streamFactory, _logService, this);
        _allocationMap = new AllocationMapService(_disk, _streamFactory, _bufferFactory);


        // full factory dependencies
        _master = new MasterService(this);
        _monitor = new TransactionMonitor(this);

    }

    #region Singleton instances (Get/Properties)

    public IEngineSettings Settings { get; }

    public EngineState State { get; private set; } = EngineState.Close;

    public Exception? Exception { get; private set; }

    public ConcurrentDictionary<string, object> Application { get; } = new();

    public IBsonReader GetBsonReader() => _bsonReader;

    public IBsonWriter GetBsonWriter() => _bsonWriter;

    public IBufferFactory GetBufferFactory() => _bufferFactory;

    public IMemoryCacheService GetMemoryCache() => _memoryCache;

    public IDataPageService GetDataPageService() => _dataPageService;

    public IIndexPageService GetIndexPageService() => _indexPageService;

    public IDiskService GetDisk() => _disk;

    public IStreamFactory GetStreamFactory() => _streamFactory;

    public IAllocationMapService GetAllocationMap() => _allocationMap;

    public IMasterService GetMaster() => _master;

    public ILogService GetLogService() => _logService;

    public IWalIndexService GetWalIndex() => _walIndex;

    public ITransactionMonitor GetMonitor() => _monitor;

    public ILockService GetLock() => _lock;

    #endregion

    #region Transient instances (Create prefix)

    public IEngineContext CreateEngineContext() 
        => new EngineContext();

    public IDiskStream CreateDiskStream() 
        => new DiskStream(this);

    public IStreamFactory CreateStreamFactory(bool readOnly)
    {
        if (this.Settings.Filename is null) throw new NotImplementedException();

        return new FileStreamFactory(this.Settings);
    }

    public INewDatafile CreateNewDatafile() 
        => new NewDatafile(this);

    public ITransaction CreateTransaction(int transactionID, byte[] writeCollections, int readVersion) 
        => new Transaction(this, transactionID, writeCollections, readVersion);

    public IDataService CreateDataService(ITransaction transaction)
        => new DataService(this, transaction);

    public IIndexService CreateIndexService(ITransaction transaction)
        => new IndexService(this, transaction);

    #region Commands

    public ICheckpointCommand CreateCheckpointCommand(IEngineContext ctx)
        => new CheckpointCommand(this, ctx);

    public IOpenCommand CreateOpenCommand(IEngineContext ctx)
        => new OpenCommand(this, ctx);

    public ICreateCollectionCommand CreateCreateCollectionCommand(IEngineContext ctx)
        => new CreateCollectionCommand(this, ctx);

    public IInsertCommand CreateInsertCommand(IEngineContext ctx)
        => new InsertCommand(this, ctx);

    #endregion

    #endregion

    #region Modified State Methods

    public void SetState(EngineState state)
    {
        if (state == EngineState.Open)
        {
            if (this.State != EngineState.Close) throw new InvalidOperationException("Engine must be closed before open");

            this.State = EngineState.Open;
        }
        else if (state == EngineState.Shutdown)
        {
            this.State = EngineState.Shutdown;
        }
        else if (state == EngineState.Close)
        {
            this.State = EngineState.Close;
        }

    }

    #endregion
}