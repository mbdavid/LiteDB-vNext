namespace LiteDB.Engine;

[AutoInterface(true)]
internal class DiskService : IDiskService
{
    private readonly EngineSettings _settings;
    private IDiskStream _writer;
    private IServicesFactory _factory;
    private readonly IMemoryFactory _memoryFactory;
    private ConcurrentQueue<IDiskStream> _readers = new ();

    public DiskService(IServicesFactory factory, IMemoryFactory memoryFactory, EngineSettings settings)
    {
        _factory = factory;
        _memoryFactory = memoryFactory;
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
        var headerBuffer = _memoryFactory.Rent();
        var ampBuffer = _memoryFactory.Rent();

        var headerPage = new HeaderPage(headerBuffer);
        var ampPage = new AllocationMapPage(1, headerBuffer);

        headerPage.GetBufferWrite();
        ampPage.GetBufferWrite();

        await _writer!.WriteAsync(0, headerBuffer.Memory);
        await _writer!.WriteAsync(PAGE_SIZE, ampBuffer.Memory);
    }
}
