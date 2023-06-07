namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task ShutdownAsync()
    {
        if (_factory.State != EngineState.Open) throw ERR("must be open");

        var lockService = _factory.GetLock();
        var diskService = _factory.GetDisk();
        var logService = _factory.GetLogService();
        var allocationMapService = _factory.GetAllocationMap();
        var writer = diskService.GetDiskWriter();

        // must enter in exclusive lock
        await lockService.EnterExclusiveAsync();

        // set engine state to shutdown
        _factory.SetState(EngineState.Shutdown);

        // do checkpoint
        await logService.CheckpointAsync(diskService, null);

        // persist all dirty amp into disk
        await allocationMapService.WriteAllChangesAsync();

        // call all dispose
        diskService.Dispose();

        // set state to close
        _factory.SetState(EngineState.Close);

        // release exclusive
        lockService.ExitExclusive();
    }
}