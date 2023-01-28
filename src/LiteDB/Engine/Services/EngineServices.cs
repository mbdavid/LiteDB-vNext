namespace LiteDB.Engine;

[GenerateAutoInterface]
internal class EngineServices : IEngineServices
{
    private readonly IServicesFactory _factory;

    public EngineState State { get; private set; }

    public IMemoryCache Cache { get; private set; }

    public EngineServices(IServicesFactory factory)
    {
        _factory = factory;
    }

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        this.Cache = _factory.CreateMemoryCache(_factory);

        this.State = EngineState.Open;

        //
        return Task.CompletedTask;
    }
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;

    }

}

