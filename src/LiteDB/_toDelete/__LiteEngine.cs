namespace LiteDB.Engine;

/// <summary>
/// A public class that take care of all engine data structure access - it´s basic implementation of a NoSql database
/// Its isolated from complete solution - works on low level only (no linq, no poco... just BSON objects)
/// [ThreadSafe]
/// </summary>
[AutoInterface(typeof(IDisposable))]
public partial class __LiteEngine : I__LiteEngine
{
    private readonly I__ServicesFactory _factory;

    public EngineState State => _factory.State;

    #region Ctor

    /// <summary>
    /// Initialize LiteEngine using in-memory database
    /// </summary>
    public __LiteEngine()
        : this(new EngineSettings { DataStream = new MemoryStream() })
    {
    }

    /// <summary>
    /// Initialize LiteEngine using file system
    /// </summary>
    public __LiteEngine(string filename)
        : this (new EngineSettings { Filename = filename })
    {
    }

    /// <summary>
    /// Initialize LiteEngine using all engine settings
    /// </summary>
    public __LiteEngine(EngineSettings settings)
        : this  (new __ServicesFactory(settings))
    {
    }

    /// <summary>
    /// To initialize LiteEngine we need classes factory and engine settings
    /// Current version still using IServiceFactory as internal...
    /// </summary>
    internal __LiteEngine(I__ServicesFactory factory)
    {
        _factory = factory;
    }

    #endregion

    // to see all methods, look at /Commands files (partial class from I__LiteEngine)

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~__LiteEngine()
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
