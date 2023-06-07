namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task OpenAsync()
    {
        if (_factory.State != EngineState.Close) throw ERR("must be closed");

        var lockService = _factory.LockService;
        var diskService = _factory.DiskService;
        var logService = _factory.LogService;
        var allocationMapService = _factory.AllocationMapService;
        var masterService = _factory.MasterService;

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
        _factory.State = EngineState.Open;

        // release exclusive
        lockService.ExitExclusive();
        // se deu erro, o state = closed
    }
}