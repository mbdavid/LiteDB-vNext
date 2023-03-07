namespace LiteDB.Engine;

[AutoInterface(true)]
internal class OpenCommand : IOpenCommand
{
    private readonly IEngineContext _ctx;

    public OpenCommand(IEngineContext ctx)
    {
        _ctx = ctx;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var state = _ctx.Services.State;
        var disk = _ctx.Services.DiskService;

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
        _ctx.Services.State = EngineState.Open;
    }
}
