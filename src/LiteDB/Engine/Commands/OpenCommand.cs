namespace LiteDB.Engine;

[AutoInterface]
internal class OpenCommand : IOpenCommand
{
    private readonly IServicesFactory _factory;
    private readonly IDiskService _disk;
    private readonly ILockService _lock;
    private readonly IMasterService _master;
    private readonly IAllocationMapService _allocationMap;

    private readonly IEngineContext _ctx;

    public OpenCommand(IServicesFactory factory, IEngineContext ctx)
    {
        _factory = factory;
        _disk = factory.GetDisk();
        _lock = factory.GetLock();
        _master = factory.GetMaster();
        _allocationMap = factory.GetAllocationMap();

        _ctx = ctx;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_factory.State != EngineState.Close) throw new Exception("must be closed");

        await _lock.EnterExclusive();

        if (_factory.State != EngineState.Close) throw new Exception("must be closed");

        // open/create data file and returns file header
        var fileHeader = await _disk.InitializeAsync();

        // testa recovery
        if (fileHeader.Recovery)
        {
            // recovey
        }

        // initialize AM service
        await _allocationMap.InitializeAsync();

        // read $master
        await _master.ReadFromDiskAsync();

        // update header/state
        _factory.SetStateOpen(fileHeader);

        // release exclusive
        _lock.ExitExclusive();
    }
}
