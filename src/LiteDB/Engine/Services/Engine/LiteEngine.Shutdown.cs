namespace LiteDB.Engine;

public partial class LiteEngine //: ILiteEngine
{
    public async Task ShutdownAsync(bool force, CancellationToken cancellationToken = default)
    {
        if (_services.State == EngineState.Shutdown) return;
        if (_services.State == EngineState.Close) throw new InvalidOperationException($"Engine is Closed.");

        // set shutdown state before any change
        _services.State = EngineState.Shutdown;

        // lock

        try
        {
            // update fps in disk

            // update header in disk
            await this.WriteHeaderDiskAsync(cancellationToken);
        }
        finally
        {
            // unlock

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
            await disk.WriteDataAsync(new[] { new PageLocation(header.GetBufferWrite(), header.PageID) }, cancellationToken);
        }
    }
}
