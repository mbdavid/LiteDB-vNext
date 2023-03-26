namespace LiteDB;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface]
internal partial class ServicesFactory : IServicesFactory
{
    private IMemoryCacheService? _memoryCache;
    private IBufferFactoryService? _bufferFactory;
    private IPageWriteFactoryService? _pageWriteFactory;
    private IIndexCacheService? _indexCache;
    private IDiskService? _disk;
    private IAllocationMapService? _allocationMap;
    private IWalIndexService? _walIndex;
    private ILockService? _lock;
    private ITransactionMonitor? _monitor;
    private IMasterService? _master;
    private IBsonReader? _bsonReader;
    private IBsonWriter? _bsonWriter;

    public ServicesFactory(IEngineSettings settings)
    {
        this.Settings = settings;
    }

    #region Singleton instances (Properties)

    public IEngineSettings Settings { get; }

    public EngineState State { get; private set; } = EngineState.Close;

    public Exception? Exception { get; private set; }

    public FileHeader? FileHeader { get; private set; }

    public ConcurrentDictionary<string, object> Application { get; } = new();

    public IBsonReader GetBsonReader()
    {
        return _bsonReader ??= new BsonReader();
    }

    public IBsonWriter GetBsonWriter()
    {
        return _bsonWriter ??= new BsonWriter();
    }

    public IMemoryCacheService GetMemoryCache()
    {
        return _memoryCache ??= new MemoryCacheService();
    }

    public IBufferFactoryService GetBufferFactory()
    {
        return _bufferFactory ??= new BufferFactoryService();
    }

    public IPageWriteFactoryService GetPageWriteFactory()
    {
        return _pageWriteFactory ??= new PageWriteFactoryService(this);
    }

    public IDiskService GetDisk()
    { 
        return _disk ??= new DiskService(this);
    }

    public IAllocationMapService GetAllocationMap()
    {
        return _allocationMap ??= new AllocationMapService(this);
    }

    public IIndexCacheService GetIndexCache()
    { 
        return _indexCache ??= new IndexCacheService();
    }

    public IMasterService GetMaster()
    {
        return _master ??= new MasterService(this);
    }

    public IWalIndexService GetWalIndex()
    {
        return _walIndex ??= new WalIndexService(this);
    }

    public ITransactionMonitor GetMonitor()
    {
        return _monitor ??= new TransactionMonitor(this);
    }

    public ILockService GetLock()
    {
        return _lock ??= new LockService(this.Settings.Timeout);
    }

    #endregion

    #region Transient instances (Create prefix)

    public IEngineContext CreateEngineContext()
    {
        return new EngineContext();
    }

    public IOpenCommand CreateOpenCommand(IEngineContext ctx)
    {
        return new OpenCommand(this, ctx);
    }

    public IDiskStream CreateDiskStream(bool readOnly)
    {
        if (this.Settings.Filename is null) throw new NotImplementedException();

        return new FileDiskStream(this.Settings);
    }

    public INewDatafile CreateNewDatafile()
    {
        return new NewDatafile(this);
    }

    public ITransactionService CreateTransaction(int transactionID)
    {
        return new TransactionService(this, transactionID);
    }

    public ISnapshot CreateSnapshot(byte colID, LockMode mode, int readVersion)
    {
        return new Snapshot(this, colID, mode, readVersion);
    }

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