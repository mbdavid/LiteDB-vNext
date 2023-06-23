namespace LiteDB;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal partial class ServicesFactory : IServicesFactory
{
    public IEngineSettings Settings { get; }
    public EngineState State { get; set; } = EngineState.Close;
    public FileHeader FileHeader { get; set; }
    public Exception? Exception { get; set; }



    public ConcurrentDictionary<string, object> Application { get; } = new();

    public IBsonReader BsonReader { get; }
    public IBsonWriter BsonWriter { get; }

    public IBufferFactory BufferFactory { get; }
    public IStreamFactory StreamFactory { get; }

    public ICacheService CacheService { get; }

    public ILogService LogService { get; }
    public IWalIndexService WalIndexService { get; }

    public ILockService LockService { get; }
    public IDiskService DiskService { get; }
    public IRecoveryService RecoveryService { get; }
    public IAllocationMapService AllocationMapService { get; }
    public IMasterMapper MasterMapper { get; }
    public IMasterService MasterService { get; }
    public IMonitorService MonitorService { get; }
    public IAutoIdService AutoIdService { get; }
    public IDataPageService DataPageService { get; }
    public IIndexPageService IndexPageService { get; }

    public ServicesFactory(IEngineSettings settings)
    {
        // get settings instance
        this.Settings = settings;

        // intial state
        this.FileHeader = new ();
        this.State = EngineState.Close;
        this.Exception = null;

        // no dependencies
        this.BsonReader = new BsonReader();
        this.BsonWriter = new BsonWriter();
        this.WalIndexService = new WalIndexService();
        this.BufferFactory = new BufferFactory();
        this.DataPageService = new DataPageService();
        this.IndexPageService = new IndexPageService();
        this.MasterMapper = new MasterMapper();
        this.AutoIdService = new AutoIdService();
        this.RecoveryService = new RecoveryService(this.BufferFactory, this.DiskService);
        // settings dependency only
        this.LockService = new LockService(settings.Timeout);
        this.StreamFactory = new FileStreamFactory(settings);

        // other services dependencies
        this.CacheService = new CacheService(this.BufferFactory);
        this.DiskService = new DiskService(this.StreamFactory, this);
        this.LogService = new LogService(this.DiskService, this.CacheService, this.BufferFactory, this.WalIndexService, this);
        this.AllocationMapService = new AllocationMapService(this.DiskService, this.BufferFactory);
        this.MasterService = new MasterService(this);
        this.MonitorService = new MonitorService(this);
    }

    #region Transient instances (Create prefix)

    public IEngineContext CreateEngineContext() 
        => new EngineContext();

    public IDiskStream CreateDiskStream()
        => new DiskStream(this.Settings, this.StreamFactory);

    public IStreamFactory CreateStreamFactory(bool readOnly)
    {
        if (this.Settings.Filename is null) throw new NotImplementedException();

        return new FileStreamFactory(this.Settings);
    }

    public INewDatafile CreateNewDatafile() => new NewDatafile(
        this.BufferFactory, 
        this.MasterMapper,
        this.BsonWriter, 
        this.DataPageService,
        this.Settings);

    public ITransaction CreateTransaction(int transactionID, byte[] writeCollections, int readVersion) => new Transaction(
        this.DiskService,
        this.LogService,
        this.BufferFactory,
        this.CacheService,
        this.WalIndexService,
        this.AllocationMapService,
        this.IndexPageService,
        this.DataPageService,
        this.LockService,
        transactionID, writeCollections, readVersion);

    public IDataService CreateDataService(ITransaction transaction) => new DataService(
        this.DataPageService, 
        this.BsonReader, 
        this.BsonWriter, 
        transaction);

    public IIndexService CreateIndexService(ITransaction transaction) => new IndexService(
        this.IndexPageService, 
        this.FileHeader.Collation, 
        transaction);

    public void Dispose()
    {
        // dispose all instances services to keep all clean (disk/memory)

        // variables/lists only
        this.WalIndexService.Dispose();
        this.LockService.Dispose();
        this.RecoveryService.Dispose();

        // pageBuffer dependencies
        this.CacheService.Dispose();
        this.LogService.Dispose();
        this.AllocationMapService.Dispose();
        this.MasterService.Dispose();
        this.MonitorService.Dispose();
        this.DiskService.Dispose();

        // dispose buffer pages
        this.BufferFactory.Dispose();

        this.State = EngineState.Close;

        // keeps "Exception" value (will be clean in next open)
        // keeps "FileHeader"
        // keeps "State"


    }

    #endregion
}