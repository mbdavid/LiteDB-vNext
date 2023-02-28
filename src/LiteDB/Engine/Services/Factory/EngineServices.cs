namespace LiteDB;


[AutoInterface]
internal class EngineServices : IEngineServices
{
    public EngineServices(IServicesFactory factory)
    {
        this.PageCacheService = factory.CreatePageCacheService();
        _factory = factory;
    }

    public EngineState State { get; set; } = EngineState.Close;

//    public EngineSettings Settings => _settings;

    public IPageCacheService PageCacheService { get; set; }

}