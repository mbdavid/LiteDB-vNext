namespace LiteDB;

// adding IDisposable in auto-generated interface IEngineServices
internal partial interface IEngineServices : IDisposable { }

/// <summary>
/// All shared services used in engine. Singleton
/// [THREAD_SAFE]
/// </summary>
[AutoInterface(true)]
internal class EngineServices : IEngineServices
{
    public EngineServices(IServicesFactory factory, EngineSettings settings)
    {
        // initalize dependecy-free services
        this.MemoryCache = factory.CreateMemoryCacheService();

        // initialize disk service (will not open file here)
        this.DiskService = factory.CreateDiskService(this.MemoryCache, settings);
    }

    public EngineState State { get; set; } = EngineState.Close;

    public Exception? Exception { get; set; }

    public ConcurrentDictionary<string, object> Application { get; } = new();

    public IDiskService DiskService { get; set; }

    public IMemoryCacheService MemoryCache { get; set; }

    public void Dispose()
    {
    }
}