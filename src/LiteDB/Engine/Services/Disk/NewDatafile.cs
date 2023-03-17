namespace LiteDB.Engine;

[AutoInterface]
internal class NewDatafile : INewDatafile
{
    private readonly IServicesFactory _factory;
    private readonly IMemoryCacheService _memoryCache;
    private readonly IEngineSettings _settings;

    public NewDatafile(IServicesFactory factory)
    {
        _factory = factory;
        _memoryCache = factory.MemoryCache;
        _settings = factory.Settings;
    }

    public async Task CreateAsync(IDiskStream stream)
    {
        var headerBuffer = _memoryCache.AllocateNewBuffer();
        var mapBuffer = _memoryCache.AllocateNewBuffer();

        var headerPage = new HeaderPage(headerBuffer);
        var mapPage = new AllocationMapPage(1, mapBuffer);

        headerPage.UpdateHeaderBuffer();
        mapPage.UpdateHeaderBuffer();

        await stream.WriteAsync(headerBuffer);
        await stream.WriteAsync(mapBuffer);

        _memoryCache.DeallocateBuffer(headerBuffer);
        _memoryCache.DeallocateBuffer(mapBuffer);

    }
}