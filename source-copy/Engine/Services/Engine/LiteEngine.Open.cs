namespace LiteDB.Engine;

public partial class LiteEngine //: ILiteEngine
{
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_services.State == EngineState.Open) return;
        if (_services.State == EngineState.Shutdown) throw new InvalidOperationException("Engine is in shutdown process. Wait for Close state.");

        // get or initialize global lock engine
        _services.Locker ??= new AsyncReaderWriterLock();

        await _services.Locker.AcquireWriterLock(cancellationToken);

        try
        {
            // double check after acquire exclusive lock
            if (_services.State == EngineState.Open) return;

            // initializing cache service
            _services.Cache = new MemoryCache();

            // initialize wal index service (log file version/position map)
            _services.WalIndex = new WalIndexService();

            // initialize disk operation
            _services.Disk = new DiskService(_services.Settings.CreateDataFactory(), _services.Settings.ReadOnly);

            // if disk is empty, create header page in memory
            if (_services.Disk.IsNew)
            {
                await this.CreateNewDatabase();
            }

            // load header from disk
            await this.ReadHeaderDiskAsync(cancellationToken);

            // verifica recovery na header?

            // init allocation map service
            _services.AllocationMap = new AllocationMapService();

            // verifica recovery
            // inicia escrita de log no disco
        }
        catch
        {
            _services.Dispose();

            throw;
        }
        finally
        {
            _services.Locker.ReleaseReaderLock();
        }
    }

    /// <summary>
    /// Read header page (#0) and initialize HeaderPage instance
    /// </summary>
    private async Task ReadHeaderDiskAsync(CancellationToken cancellationToken = default)
    {
        // read first page with header information. Keep global HeaderPage instance in _services.Header
        var buffer = new BufferPage(false);

        using (var reader = _services.Disk.GetReader())
        {
            await reader.ReadPageAsync(buffer.Memory, 0, cancellationToken);
        }

        _services.Header = new HeaderPage(buffer);
    }

    /// <summary>
    /// Create a new database with 3 pages: Header, AllocationMap, MasterDataPage
    /// Store on disk (don't use cache)
    /// </summary>
    private async Task CreateNewDatabase(CancellationToken cancellationToken = default)
    {
        using var header = new HeaderPage();
        using var allocationMap = new AllocationMapPage(AMP_FIRST_PAGE_ID);
        using var masterPage = new BlockPage(MASTER_PAGE_ID, PageType.Data, MASTER_COL_ID);

        var content = new BsonDocument(); //TODO: inicializar

        var length = content.GetBytesCount();
        var buffer = new byte[length]; // use few bytes only.. no big deal (run once)

        //TODO: usar dataPage com DataService?;

        //allocationMap.Update(MASTER_COL_ID, MASTER_PAGE_ID, PageType.Data, 8160);

        var disk = _services.Disk;

        // write both 3 pages 
        await disk.WriteDataAsync(new PageDataLocation[] { 
            new (header.PageID, header.GetBufferWrite()),
            new (allocationMap.PageID, allocationMap.GetBufferWrite()),
            new (masterPage.PageID, masterPage.GetBufferWrite())
        }, cancellationToken);

        // all 3 pages are disposabled here
    }
}
