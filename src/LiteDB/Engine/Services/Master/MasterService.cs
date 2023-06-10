namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class MasterService : IMasterService
{
    // dependency injection
    private readonly IServicesFactory _factory;

    /// <summary>
    /// $master document object model in memory
    /// </summary>
    private MasterDocument _master;

    private readonly IMasterMapper _mapper;

    public MasterService(IServicesFactory factory)
    {
        _factory = factory;
        _master = new();

        _mapper = _factory.MasterMapper;
    }

    #region Read/Write $master

    /// <summary>
    /// Initialize (when database open) reading first extend pages. Database should have no log data to read this
    /// Initialize _master document instance
    /// </summary>
    public async Task InitializeAsync()
    {
        // create a a local transaction (not from monitor)
        using var transaction = _factory.CreateTransaction(0, new byte[0], 0);

        // initialize data service with new transaction
        var dataService = _factory.CreateDataService(transaction);

        // read $master document
        var doc = await dataService.ReadDocumentAsync(MASTER_ROW_ID, null);

        // convert bson into MasterMapper and set as current master
        _master = _mapper.MapToMaster(doc);
    }

    /// <summary>
    /// Get Master document from memory to readOnly or for write (get a clone object)
    /// </summary>
    public MasterDocument GetMaster(bool writable)
    {
        return writable ? new MasterDocument(_master) : _master; 
    }


    /// <summary>
    /// Set a new (in-memory) master object to Master service
    /// </summary>
    public void SetMaster(MasterDocument master)
    {
        _master = master;
    }

    /// <summary>
    /// Write all master document into page buffer and write on this. Must use a real transaction
    /// to store all pages into log
    /// </summary>
    public async Task WriteCollectionAsync(MasterDocument master, ITransaction transaction)
    {
        var dataService = _factory.CreateDataService(transaction);

        var doc = _mapper.MapToDocument(master);

        await dataService.UpdateDocumentAsync(MASTER_ROW_ID, doc);
    }

    #endregion

    public void Dispose()
    {
    }
}
