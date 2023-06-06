namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task OpenAsync()
    {
        if (_factory.State != EngineState.Close) throw ERR("must be closed");

        var lockService = _factory.GetLock();
        var diskService = _factory.GetDisk();
        var logService = _factory.GetLogService();
        var allocationMapService = _factory.GetAllocationMap();
        var masterService = _factory.GetMaster();

        await lockService.EnterExclusiveAsync();

        if (_factory.State != EngineState.Close) throw ERR("must be closed");

        // open/create data file and returns file header
        var fileHeader = await diskService.InitializeAsync();

        if (fileHeader.Recovery) throw new NotImplementedException();

        // initialize log service
        logService.Initialize(diskService.GetLastFilePositionID());

        // initialize AM service
        await allocationMapService.InitializeAsync();

        // read $master
        await masterService.InitializeAsync();

        // update header/state
        _factory.SetState(EngineState.Open);

        // release exclusive
        lockService.ExitExclusive();
        // se deu erro, o state = closed
    }
}