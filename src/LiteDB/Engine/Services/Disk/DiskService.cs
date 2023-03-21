namespace LiteDB.Engine;

/// <summary>
/// Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class DiskService : IDiskService
{
    // dependency injection
    private readonly IServicesFactory _factory;
    private readonly IMemoryCacheService _memoryCache;

    private IDiskStream _writer;
    private readonly PageBuffer _headerBuffer;
    private readonly ConcurrentQueue<IDiskStream> _readers = new ();

    public DiskService(IServicesFactory factory)
    {
        _factory = factory;
        _memoryCache = factory.MemoryCache;

        _headerBuffer = _memoryCache.AllocateNewBuffer();
        _writer = factory.CreateDiskStream(false);
    }

    /// <summary>
    /// Open (or create) datafile. Checks file integrity based on recovery flag.
    /// Returns false if disk was not dispoable after last write use (recovery = true)
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        // if file not found, create empty database
        if (_writer.Exists() == false)
        {
            var df = _factory.CreateNewDatafile();

            await df.CreateAsync(_writer);
        }

        // read header page buffer from start of disk
        var read = await _writer.ReadPageAsync(0, _headerBuffer);

        ENSURE(read, "page header must be full read");



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

            await _writer.ReadPageAsync(position, pageBuffer);

            if (pageBuffer.IsHeaderEmpty()) break;

            yield return pageBuffer;

            position += (AM_PAGE_STEP * PAGE_SIZE);
        }
    }

    /// <summary>
    /// Rent a disk reader from pool. Must return after use
    /// </summary>
    public IDiskStream RentDiskReader()
    {
        if (_readers.TryDequeue(out var reader))
        {
            return reader;
        }

        return _factory.CreateDiskStream(true);
    }

    /// <summary>
    /// Return a rented reader and add to pool
    /// </summary>
    public void ReturnDiskReader(IDiskStream reader)
    {
        _readers.Enqueue(reader);
    }

    public void Dispose()
    {
        //TODO: implementar fechamento de todos os streams
        // desalocar header
    }
}
