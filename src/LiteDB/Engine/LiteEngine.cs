namespace LiteDB.Engine;

/// <summary>
/// A public class that take care of all engine data structure access - it´s basic implementation of a NoSql database
/// Its isolated from complete solution - works on low level only (no linq, no poco... just BSON objects)
/// [ThreadSafe]
/// </summary>
[AutoInterface(typeof(IDisposable))]
public partial class LiteEngine : ILiteEngine
{
    private readonly IServicesFactory _factory;

    public EngineState State => _factory.State;

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
        : this  (new ServicesFactory(settings))
    {
    }

    /// <summary>
    /// To initialize LiteEngine we need classes factory and engine settings
    /// Current version still using IServiceFactory as internal...
    /// </summary>
    internal LiteEngine(IServicesFactory factory)
    {
        _factory = factory;
    }

    #endregion

    #region Open/Close database

    public async Task<bool> OpenAsync()
    {
        using var ctx = _factory.CreateEngineContext();

        var open = _factory.CreateOpenCommand(ctx);

        await open.ExecuteAsync();

        return true;
    }

    public async Task<bool> CreateCollectionAsync(string collectionName)
    {
        using var ctx = _factory.CreateEngineContext();

        var create = _factory.CreateCreateCollectionCommand(ctx);

        await create.ExecuteAsync(collectionName);

        return true;
    }

    public async Task<int> InsertAsync(string collectionName, BsonDocument document)
    {
        using var ctx = _factory.CreateEngineContext();

        var create = _factory.CreateInsertCommand(ctx);

        await create.ExecuteAsync(collectionName, document);

        return 0;
    }

    public async Task<bool> CheckpointAsync()
    {
        using var ctx = _factory.CreateEngineContext();

        var create = _factory.CreateCheckpointCommand(ctx);

        await create.ExecuteAsync();

        return true;
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
