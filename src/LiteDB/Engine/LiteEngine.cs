namespace LiteDB.Engine;

// adding IDisposable in auto-generated interface ILiteEngine
public partial interface ILiteEngine : IDisposable { }

/// <summary>
/// A public class that take care of all engine data structure access - it´s basic implementation of a NoSql database
/// Its isolated from complete solution - works on low level only (no linq, no poco... just BSON objects)
/// [ThreadSafe]
/// </summary>
[AutoInterface]
public partial class LiteEngine : ILiteEngine
{
    private readonly IEngineServices _services;
    private readonly IServicesFactory _factory;

    public EngineState State => _services.State;

    #region Ctor

    /// <summary>
    /// Initialize LiteEngine using in-memory database
    /// </summary>
    public LiteEngine()
        : this(new EngineSettings { DataStream = new MemoryStream() })
    {
    }

    /// <summary>
    /// Initialize LiteEngine using file system
    /// </summary>
    public LiteEngine(string filename)
        : this (new EngineSettings { Filename = filename })
    {
    }

    /// <summary>
    /// Initialize LiteEngine using all engine settings
    /// </summary>
    public LiteEngine(EngineSettings settings)
        : this  (new ServicesFactory(), settings)
    {
    }

    /// <summary>
    /// To initialize LiteEngine we need classes factory and engine settings
    /// Current version still using IServiceFactory as internal...
    /// </summary>
    internal LiteEngine(IServicesFactory factory, EngineSettings settings)
    {
        _factory = factory;
        _services = _factory.CreateEngineServices(settings);
    }

    #endregion

    #region Open/Close database

    public async Task OpenAsync()
    {
        using var ctx = _factory.CreateEngineContext(_services);

        var open = _factory.CreateOpenCommand(ctx);

        await open.ExecuteAsync();
    }

    #endregion

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~LiteEngine()
    {
        this.Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        //if (_disposed) return;

        if (disposing)
        {
            //_services.CloseAsync().Wait;
        }

        //_disposed = true;
    }
}
