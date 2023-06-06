namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task CloseAsync()
    {
        if (_factory.State != EngineState.Open) throw ERR("must be open");

        var lockService = _factory.GetLock();
        var diskService = _factory.GetDisk();
        var logService = _factory.GetLogService();

        await lockService.EnterExclusiveAsync();




        // release exclusive
        lockService.ExitExclusive();
    }
}