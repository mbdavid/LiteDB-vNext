namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<int> CheckpointAsync()
    {
        if (_factory.State != EngineState.Open) throw ERR("must be opened");

        var lockService = _factory.LockService;
        var logService = _factory.LogService;

        // checkpoint require exclusive lock (no readers/writers)
        await lockService.EnterExclusiveAsync();

        // do checkpoint and returns how many pages was overrided
        var result = logService.Checkpoint(false);

        // release exclusive
        lockService.ExitExclusive();

        return result;
    }
}