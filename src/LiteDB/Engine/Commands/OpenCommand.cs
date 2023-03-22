namespace LiteDB.Engine;

[AutoInterface]
internal class OpenCommand : IOpenCommand
{
    private readonly IServicesFactory _factory;
    private readonly IDiskService _disk;
    private readonly IEngineContext _ctx;

    public OpenCommand(IServicesFactory factory, IEngineContext ctx)
    {
        _factory = factory;
        _disk = factory.GetDisk();
        _ctx = ctx;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var state = _factory.State; // state is an enum (valueType) - must be read direct from _factory

        // lock exclusivo? var exclusive = _ctx.Services.Locker.TryExclusive()

        if (state != EngineState.Close) throw new Exception("must be closed");

        var fileHeader = await _disk.InitializeAsync();


        // update state
        _factory.State = EngineState.Open;
    }
}
