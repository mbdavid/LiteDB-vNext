namespace LiteDB.Engine;

public partial class LiteEngine //: ILiteEngine
{
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (_services.State == EngineState.Shutdown) return;
        if (_services.State == EngineState.Close) throw new InvalidOperationException($"Engine is Closed.");

        // set shutdown state before any change
        _services.State = EngineState.Shutdown;

        var locker = _services.Locker;

        // requires exclusive mode
        await locker.AcquireWriterLock(cancellationToken);

        try
        {
            // update fps on disk

            // update header on disk
            await this.WriteHeaderDiskAsync(cancellationToken);
        }
        finally
        {
            // release exclusive mode
            locker.ReleaseWriterLock();

            // Dispose() all services and set State = Closed
            _services.Dispose();
        }
    }

    /// <summary>
    /// Write header page into disk (if was changed)
    /// </summary>
    private async Task WriteHeaderDiskAsync(CancellationToken cancellationToken = default)
    {
        var header = _services.Header;
        var disk = _services.Disk;

        if (header.IsDirty)
        {
            // header page stores direct into Data space (do not use log)
            await disk.WriteDataAsync(new[] { new PageDataLocation(header.PageID, header.GetBufferWrite()) }, cancellationToken);
        }
    }
}
