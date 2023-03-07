namespace LiteDB.Engine;

[AutoInterface(true)]
internal class DiskService : IDiskService
{
    private readonly EngineSettings _settings;
    private IDiskStream _writer;
    private IServicesFactory _factory;
    private readonly IMemoryCacheService _memoryCache;
    private ConcurrentQueue<IDiskStream> _readers = new ();

    public DiskService(IServicesFactory factory, IMemoryCacheService memoryCache, EngineSettings settings)
    {
        _factory = factory;
        _memoryCache = memoryCache;
        _settings = settings;

        _writer = _factory.CreateDiskStream(_settings, true);
    }

    public async Task<bool> InitializeAsync()
    {
        if (_writer.Exists() == false)
        {
            await this.CreateNewDatafileAsync();
        }

        // abre o arquivo e retorna true se está tudo ok e não precisa de recovery

        return true;
    }

    private async Task CreateNewDatafileAsync()
    {
        var headerBuffer = _memoryCache.AllocateNewPage();
        var ampBuffer = _memoryCache.AllocateNewPage();

        var headerPage = new HeaderPage(headerBuffer);
        var ampPage = new AllocationMapPage(1, headerBuffer);

        headerPage.GetBufferWrite();
        ampPage.GetBufferWrite();

        await _writer!.WriteAsync(headerBuffer);
        await _writer!.WriteAsync(ampBuffer);

        _memoryCache.DeallocatePage(headerBuffer);
        _memoryCache.DeallocatePage(ampBuffer);
    }
}
