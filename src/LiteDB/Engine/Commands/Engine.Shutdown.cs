namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task ShutdownAsync()
    {
        if (_factory.State != EngineState.Open) throw ERR("must be open");

        var lockService = _factory.LockService;
        var diskService = _factory.DiskService;
        var logService = _factory.LogService;
        var allocationMapService = _factory.AllocationMapService;
        var writer = diskService.GetDiskWriter();

        // must enter in exclusive lock
        await lockService.EnterExclusiveAsync();

        // set engine state to shutdown
        _factory.State = EngineState.Shutdown;

        // do checkpoint
        await logService.CheckpointAsync(diskService, 100, null, true);

        // persist all dirty amp into disk
        await allocationMapService.WriteAllChangesAsync();

        // if file was changed, update file header check byte
        if (_factory.FileHeader.IsFileDirty)
        {
            writer.WriteFlag(FileHeader.P_IS_FILE_DIRTY, 0);
        }

        // release exclusive
        lockService.ExitExclusive();

        // call sync dispose (release disk/memory)
        _factory.Dispose();
    }
}