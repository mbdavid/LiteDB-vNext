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
            // initializing cache service
            _services.Cache = new MemoryCache();

            // initialize wal index service (log file version/position map)
            _services.WalIndex = new WalIndexService();

            // initialize disk operation
            _services.Disk = new DiskService(_services.Settings.CreateDataFactory(), _services.Settings.ReadOnly);

            // if disk is empty, create header page in memory
            if (_services.Disk.IsNew)
            {
                // tem q criar o banco fisico
            }

            // load header from disk
            await this.ReadHeaderDiskAsync(cancellationToken);

            // verifica recovery na header?

            // init allocation map service
            _services.AllocationMap = new AllocationMapService();

            // verifica recovery
            // inicia escrita de log no disco
        }
        finally
        {
            _services.Locker.ReleaseReaderLock();

            _services.Dispose();
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
}
