namespace LiteDB.Engine;

[AutoInterface]
internal class CheckpointCommand : ICheckpointCommand
{
    private readonly IServicesFactory _factory;
    private readonly IDiskService _disk;
    private readonly ILockService _lock;
    private readonly IMasterService _master;
    private readonly IAllocationMapService _allocationMap;

    private readonly IEngineContext _ctx;

    public CheckpointCommand(IServicesFactory factory, IEngineContext ctx)
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
        if (_factory.State == EngineState.Open) throw ERR("must be closed");

        await _lock.EnterExclusiveAsync();




        // release exclusive
        _lock.ExitExclusive();
    }
}