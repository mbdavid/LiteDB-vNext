namespace LiteDB;

/// <summary>
/// All shared services used in engine. Singleton
/// [THREAD_SAFE]
/// </summary>
[AutoInterface(true)]
internal class EngineServices : IEngineServices
{
    private readonly EngineSettings _settings;

    public EngineServices(IServicesFactory factory, EngineSettings settings)
    {
        // initalize dependecy-free services
        this.MemoryFactory = factory.CreateMemoryFactory();
        this.PageCacheService = factory.CreatePageCacheService();

        // initialize disk service (will not open file here)
        this.DiskService = factory.CreateDiskService(this.MemoryFactory, settings);

        _settings = settings;
    }
    public EngineState State { get; set; } = EngineState.Close;

    public Exception? Exception { get; set; }

    //    public EngineSettings Settings => _settings;
    public IDiskService DiskService { get; set; }

    public IMemoryFactory MemoryFactory { get; set; }

    public IPageCacheService PageCacheService { get; set; }

}