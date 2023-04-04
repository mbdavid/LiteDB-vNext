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
    private readonly IIndexCacheService _indexCache;
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

        // initialize per-engine classes instances
        // no dependencies
        _bsonReader = new BsonReader();
        _bsonWriter = new BsonWriter();
        _memoryCache = new MemoryCacheService();
        _indexCache = new IndexCacheService();
        _walIndex = new WalIndexService();

        // settings dependency only
        _lock = new LockService(settings.Timeout);
        _streamFactory = new FileStreamFactory(settings);

        // other services dependencies
        _bufferFactory = new BufferFactory(_memoryCache);
        _disk = new DiskService(settings, _bufferFactory, _streamFactory, this);
        _allocationMap = new AllocationMapService(_disk, _streamFactory, _bufferFactory);
        _master = new MasterService(_disk, _bufferFactory, _bsonReader, _bsonWriter);
        _monitor = new TransactionMonitor(this);

        // create single instance of PageService to be used in both data/index page services
        var pageService = new PageService();

        _dataPageService = new DataPageService(pageService);
        _indexPageService = new IndexPageService(pageService);

    }

    #region Singleton instances (Get/Properties)

    public IEngineSettings Settings { get; }

    public EngineState State { get; private set; } = EngineState.Close;

    public Exception? Exception { get; private set; }

    public FileHeader? FileHeader { get; private set; }

    public ConcurrentDictionary<string, object> Application { get; } = new();

    public IBsonReader GetBsonReader() => _bsonReader;

    public IBsonWriter GetBsonWriter() => _bsonWriter;

    public IBufferFactory GetBufferFactory() => _bufferFactory;

    public IMemoryCacheService GetMemoryCache() => _memoryCache;

    public IIndexCacheService GetIndexCache() => _indexCache;

    public IDataPageService GetDataPageService() =>_dataPageService;

    public IIndexPageService GetIndexPageService() => _indexPageService;

    public IDiskService GetDisk() => _disk;

    public IStreamFactory GetStreamFactory() => _streamFactory;

    public IAllocationMapService GetAllocationMap() => _allocationMap;

    public IMasterService GetMaster() => _master;

    public IWalIndexService GetWalIndex() => _walIndex;

    public ITransactionMonitor GetMonitor() => _monitor;

    public ILockService GetLock() => _lock;

    #endregion

    #region Transient instances (Create prefix)

    public IEngineContext CreateEngineContext() 
        => new EngineContext();

    public IOpenCommand CreateOpenCommand(IEngineContext ctx) 
        => new OpenCommand(this, ctx);

    public IDiskStream CreateDiskStream() 
        => new DiskStream(this);

    public IStreamFactory CreateStreamFactory(bool readOnly)
    {
        if (this.Settings.Filename is null) throw new NotImplementedException();

        return new FileStreamFactory(this.Settings);
    }

    public INewDatafile CreateNewDatafile() 
        => new NewDatafile(this);

    public ITransaction CreateTransaction(int transactionID, byte[] writeCollections) 
        => new Transaction(this, transactionID, writeCollections);


    #endregion

    #region Modified State Methods

    public void SetStateOpen(FileHeader header)
    {
        if (this.State != EngineState.Close) throw new InvalidOperationException("Engine must be closed before open");

        this.FileHeader = header;
        this.State = EngineState.Open;
    }

    #endregion
}