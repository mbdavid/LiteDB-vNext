namespace LiteDB.Engine;

internal class ServicesFactory : IServicesFactory
{
    public IEngineServices CreateEngineServices(EngineSettings settings, IServicesFactory factory)

    {
        throw new NotImplementedException();
    }

    public IMemoryCache CreateMemoryCache(IServicesFactory factory)
        => new MemoryCache(factory);

    public IMemoryCachePage CreateMemoryCachePage(BasePage page)
        => new MemoryCachePage(page);
}
