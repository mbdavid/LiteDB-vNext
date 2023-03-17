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

    public ServicesFactory(IEngineSettings settings)
    {
        this.Settings = settings;
    }

    #region Singleton instances (Properties)

    public IEngineSettings Settings { get; }

    public EngineState State { get; set; } = EngineState.Close;

    public Exception? Exception { get; set; }

    public ConcurrentDictionary<string, object> Application { get; } = new();

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

    public IIndexCacheService IndexCache
    {
        get
        {
            return _indexCache ??= new IndexCacheService();
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

    public IDiskStream CreateDiskStream(bool sequential)
    {
        if (this.Settings.Filename is null) throw new NotImplementedException();

        return new FileDiskStream(
            this.Settings.Filename,
            this.Settings.Password,
            this.Settings.ReadOnly,
            sequential);
    }

    public INewDatafile CreateNewDatafile()
    {
        return new NewDatafile(this);
    }

    #endregion

}