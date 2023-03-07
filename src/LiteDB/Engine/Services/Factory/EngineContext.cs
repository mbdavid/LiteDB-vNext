namespace LiteDB;

// adding IDisposable in auto-generated interface IEngineContext
internal partial interface IEngineContext : IDisposable { }

[AutoInterface(true)]
internal class EngineContext : IEngineContext
{
    public Dictionary<string, object> Request { get; }

    public IServicesFactory Factory { get; }

    public IEngineServices Services { get; }

    public EngineContext(IServicesFactory factory, IEngineServices services)
    {
        this.Request = new ();
        this.Factory = factory;
        this.Services = services;
    }

    public void Dispose()
    {
    }
}

