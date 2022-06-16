namespace LiteDB.Engine;

public partial class LiteEngine //: ILiteEngine
{
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_services.State == EngineState.Open) return;
        if (_services.State == EngineState.Shutdown) throw new InvalidOperationException("Engine is in shutdown process. Wait for Close state.");

        _services.Locker = new AsyncReaderWriterLock();

        await _services.Locker.AcquireWriterLock(cancellationToken);

        try
        {
            // initializing services
            _services.Cache = new MemoryCache();

            // initialize disk operation
            _services.Disk = new DiskService(_services.Settings.CreateDataFactory(), _services.Settings.ReadOnly);

            // if disk is empty, create header page in memory
            if (_services.Disk.IsNew)
            {
                _services.Header = new HeaderPage();
            }
            else
            {
                // read first page with header information. Keep global HeaderPage instance in _services.Header
                var buffer = new BufferPage(false);

                using (var reader = _services.Disk.GetReader())
                {
                    await reader.ReadPageAsync(buffer.Memory, 0, cancellationToken);
                }

                _services.Header = new HeaderPage(buffer);
            }

            // verifica recovery
            // inicia wal
            // inicia escrita de log no disco
        }
        finally
        {
            _services.Locker.ReleaseReaderLock();
        }



    }
}
