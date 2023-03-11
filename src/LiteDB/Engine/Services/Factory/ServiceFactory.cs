namespace LiteDB;

[AutoInterface]
internal partial class ServicesFactory : IServicesFactory
{
    public IDiskStream CreateDiskStream(EngineSettings settings, bool sequential)
    {
        if (settings.Filename is null) throw new NotImplementedException();

        return new FileDiskStream(
            settings.Filename, 
            settings.Password, 
            settings.ReadOnly, 
            sequential);
    }

    public IEngineServices CreateEngineServices(EngineSettings settings)
        => new EngineServices(this, settings);
    public IMemoryCacheService CreateMemoryCacheService()
        => new MemoryCacheService();
    public IEngineContext CreateEngineContext(IEngineServices services)
        => new EngineContext(this, services);
    public IDiskService CreateDiskService(IMemoryCacheService memoryCache, EngineSettings settings)
        => new DiskService(this, memoryCache, settings);
    public IOpenCommand CreateOpenCommand(IEngineContext ctx)
        => new OpenCommand(ctx);
    public IIndexCacheService CreateIndexCacheService()
        => new IndexCacheService();
}