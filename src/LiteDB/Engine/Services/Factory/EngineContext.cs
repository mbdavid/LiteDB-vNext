namespace LiteDB;

// adding IDisposable in auto-generated interface IEngineContext
internal partial interface IEngineContext : IDisposable { }

[AutoInterface(true)]
internal class EngineContext : IEngineContext
{
    private static ConcurrentDictionary<string, object> _application = new();

    public ConcurrentDictionary<string, object> Application { get; }

    public Dictionary<string, object> Request { get; }

    public IServicesFactory Factory { get; }

    public IEngineServices Services { get; }

    public EngineContext(IServicesFactory factory, IEngineServices services)
    {
        this.Application = _application;
        this.Request = new ();
        this.Factory = factory;
        this.Services = services;
    }

    public void Dispose()
    {
    }
}

