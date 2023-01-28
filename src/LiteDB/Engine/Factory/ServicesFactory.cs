namespace LiteDB.Engine;

[GenerateAutoInterface]
internal class ServicesFactory : IServicesFactory
{
    public EngineSettings Settings { get; }

    public ServicesFactory(EngineSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        this.Settings = settings;
    }

    public IEngineServices CreateEngineServices(IServicesFactory factory)
        => new EngineServices(factory);

    public IMemoryCacheService CreateMemoryCacheService()
        => new MemoryCacheService();
}
