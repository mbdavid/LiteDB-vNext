namespace LiteDB.Engine;

internal interface IServicesFactory
{
    IEngineServices CreateEngineServices(EngineSettings settings, IServicesFactory factory);

    IMemoryCache CreateMemoryCache(IServicesFactory factory);
    IMemoryCachePage CreateMemoryCachePage(BasePage page);
    
}
