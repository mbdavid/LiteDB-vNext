namespace LiteDB.Engine;

[AutoInterface(true)]
internal class OpenCommand : IOpenCommand
{
    private readonly RequestContext _factory;

    public OpenCommand(RequestContext factory)
    {
        _factory = factory;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        //_factory.DiskService.Open();

        //
        await Task.CompletedTask;
    }
}
