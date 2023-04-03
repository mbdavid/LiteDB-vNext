namespace LiteDB.Engine;

/// <summary>
/// Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class DiskService : IDiskService
{
    // dependency injection
    private readonly IServicesFactory _factory;
    private readonly IBufferFactoryService _bufferFactory;
    private readonly IStreamFactory _streamFactory;
    private readonly IEngineSettings _settings;

    private IFileDiskStream _writer;
    private readonly ConcurrentQueue<IFileDiskStream> _readers = new ();

    public DiskService(IServicesFactory factory)
    {
        _factory = factory;
        _bufferFactory = factory.GetBufferFactory();
        _streamFactory = factory.GetStreamFactory();
        _settings = factory.Settings;

        _writer = factory.CreateFileDiskStream(false);
    }

    /// <summary>
    /// Open (or create) datafile.
    /// </summary>
    public async Task<FileHeader> InitializeAsync()
    {
        // if file not exists, create empty database
        if (_streamFactory.Exists() == false)
        {
            var fileHeader = new FileHeader(_settings);

            // create new file and write 
            await _writer.CreateAsync(fileHeader);

            // intialize new database class factory
            var newFile = _factory.CreateNewDatafile();

            // create first AM page and $master 
            await newFile.CreateAsync(_writer);

            return fileHeader;
        }
        else
        {
            // read header page buffer from start of disk
            var fileHeader = await _writer.OpenAsync(true);

            return fileHeader;
        }
    }

    /// <summary>
    /// Read all allocation map pages. Allocation map pages contains initial position and fixed interval between other pages
    /// </summary>
    public async IAsyncEnumerable<PageBuffer> ReadAllocationMapPages()
    {
        long position = AM_FIRST_PAGE_ID * PAGE_SIZE;

        // using writer stream because this reads occurs on database open
        var fileLength = _streamFactory.GetLength();

        while (position < fileLength)
        {
            var pageBuffer = _bufferFactory.AllocateNewBuffer();

            await _writer.ReadPageAsync(position, pageBuffer);

            if (pageBuffer.IsHeaderEmpty()) break;

            yield return pageBuffer;

            position += (AM_PAGE_STEP * PAGE_SIZE);
        }
    }

    /// <summary>
    /// Rent a disk reader from pool. Must return after use
    /// </summary>
    public IFileDiskStream RentDiskReader()
    {
        if (_readers.TryDequeue(out var reader))
        {
            return reader;
        }

        return _factory.CreateFileDiskStream(true);
    }

    /// <summary>
    /// Return a rented reader and add to pool
    /// </summary>
    public void ReturnDiskReader(IFileDiskStream reader)
    {
        _readers.Enqueue(reader);
    }

    public async Task WritePages(IEnumerable<PageBuffer> pages)
    {
        foreach(var pageBuffer in pages)
        {
            await _writer.WritePageAsync(pageBuffer);
        }
        await _writer.FlushAsync();
    }

    public void Dispose()
    {
        //TODO: implementar fechamento de todos os streams
        // desalocar header
    }
}
