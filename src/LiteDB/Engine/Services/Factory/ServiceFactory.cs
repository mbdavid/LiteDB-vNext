namespace LiteDB;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface]
internal partial class ServicesFactory : IServicesFactory
{
    private IMemoryCacheService? _memoryCache;
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

    public EngineState State { get; set; } = EngineState.Close;

    public Exception? Exception { get; set; }

    public ConcurrentDictionary<string, object> Application { get; } = new();

    public IBsonReader BsonReader
    {
        get
        {
            return _bsonReader ??= new BsonReader();
        }
    }

    public IBsonWriter BsonWriter
    {
        get
        {
            return _bsonWriter ??= new BsonWriter();
        }
    }

    public IMemoryCacheService MemoryCache
    {
        get
        {
            return _memoryCache ??= new MemoryCacheService();
        }
    }

    public IDiskService Disk
    {
        get
        {
            return _disk ??= new DiskService(this);
        }
    }

    public IAllocationMapService AllocationMap
    {
        get
        {
            return _allocationMap ??= new AllocationMapService(this);
        }
    }

    public IIndexCacheService IndexCache
    {
        get
        {
            return _indexCache ??= new IndexCacheService();
        }
    }

    public IMasterService Master
    {
        get
        {
            return _master ??= new MasterService(this);
        }
    }

    public IWalIndexService WalIndex
    {
        get
        {
            return _walIndex ??= new WalIndexService(this);
        }
    }

    public ITransactionMonitor Monitor
    {
        get
        {
            return _monitor ??= new TransactionMonitor(this);
        }
    }

    public ILockService Lock
    {
        get
        {
            return _lock ??= new LockService(this.Master.Document.Pragmas.Timeout);
        }
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

        // when write mode, use sequencial disk access
        var sequential = !readOnly;

        return new FileDiskStream(
            this.Settings.Filename,
            this.Settings.Password,
            readOnly || this.Settings.ReadOnly,
            sequential);
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

}