namespace LiteDB.Engine;

[AutoInterface]
internal class OpenCommand : IOpenCommand
{
    private readonly IServicesFactory _factory;
    private readonly IEngineContext _ctx;

    public OpenCommand(IServicesFactory factory, IEngineContext ctx)
    {
        _factory = factory;
        _ctx = ctx;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var state = _factory.State;
        var disk = _factory.Disk;

        // lock exclusivo? var exclusive = _ctx.Services.Locker.TryExclusive()

        if (state != EngineState.Close) throw new Exception("must be closed");

        // abre efetivamente o arquivo, cria se necessário, retorna false case seja recovery
        var recovery = !await disk.InitializeAsync();

        if (recovery)
        {
            // state = Recovering?
            // faz recovery e executa novamente o disk.Initialize()
        }

        // update state
        _factory.State = EngineState.Open;
    }
}
