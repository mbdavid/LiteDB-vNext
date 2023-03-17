namespace LiteDB.Engine;

/// <summary>
/// Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class DiskService : IDiskService
{
    private readonly IServicesFactory _factory;
    private readonly IMemoryCacheService _memoryCache;

    private IDiskStream _writer;
    private ConcurrentQueue<IDiskStream> _readers = new ();

    public DiskService(IServicesFactory factory)
    {
        _memoryCache = factory.MemoryCache;
        _factory = factory;

        _writer = _factory.CreateDiskStream(true);
    }

    public async Task<bool> InitializeAsync()
    {
        // if file not found, create empty database
        if (_writer.Exists() == false)
        {
            var df = _factory.CreateNewDatafile();

            await df.CreateAsync(_writer);
        }

        //TOD: abrir stream e retorna true se está tudo ok e não precisa de recovery

        return true;
    }

    /// <summary>
    /// Read all allocation map pages. Allocation map pages contains initial position and fixed interval between other pages
    /// </summary>
    public async IAsyncEnumerable<PageBuffer> ReadAllocationMapPages()
    {
        long position = AM_FIRST_PAGE_ID * PAGE_SIZE;

        // using writer stream because this reads occurs on database open
        var fileLength = _writer.GetLength();

        while (position < fileLength)
        {
            var pageBuffer = _memoryCache.AllocateNewBuffer();

            await _writer.ReadAsync(position, pageBuffer);

            if (pageBuffer.IsHeaderEmpty()) break;

            yield return pageBuffer;

            position += (AM_PAGE_STEP * PAGE_SIZE);
        }
    }

    private async Task CreateNewDatafileAsync()
    {
    }

    public void Dispose()
    {
        //TODO: implementar fechamento de todos os streams
    }
}
