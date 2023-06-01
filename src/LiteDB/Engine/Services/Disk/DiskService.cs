namespace LiteDB.Engine;

/// <summary>
/// Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class DiskService : IDiskService
{
    // dependency injection
    private readonly IServicesFactory _factory;
    private readonly IBufferFactory _bufferFactory;
    private readonly IStreamFactory _streamFactory;
    private readonly IEngineSettings _settings;

    private readonly IDiskStream _writer;
    private readonly ConcurrentQueue<IDiskStream> _readers = new ();

    private uint _logStartPositionID;
    private int _logEndPositionID;

    private bool _writeInit = false;

    /// <summary>
    /// Get start log position ID
    /// </summary>
    public uint LogStartPositionID => _logStartPositionID;

    /// <summary>
    /// Get end log position ID
    /// </summary>
    public uint LogEndPositionID => (uint)_logEndPositionID;

    /// <summary>
    /// Get all reference pages that are in log and must be 
    /// </summary>
    public List<uint> LogPages;

    public DiskService(IEngineSettings settings, 
        IBufferFactory bufferFactory,
        IStreamFactory streamFactory,
        IServicesFactory factory)
    {
        _settings = settings;
        _bufferFactory = bufferFactory;
        _streamFactory = streamFactory;
        _factory = factory;

        _writer = factory.CreateDiskStream();
    }

    /// <summary>
    /// Open (or create) datafile.
    /// </summary>
    public async Task<FileHeader> InitializeAsync()
    {
        // if file not exists, create empty database
        if (_streamFactory.Exists() == false)
        {
            // intialize new database class factory
            var newFile = _factory.CreateNewDatafile();

            // create first AM page and $master 
            var fileHeader = await newFile.CreateAsync(_writer);

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
    /// Rent a disk reader from pool. Must return after use
    /// </summary>
    public async Task<IDiskStream> RentDiskReaderAsync()
    {
        if (_readers.TryDequeue(out var reader))
        {
            return reader;
        }

        // create new diskstream
        reader = _factory.CreateDiskStream();

        // and open to read-only
        await reader.OpenAsync(false);

        return reader;
    }

    /// <summary>
    /// Return a rented reader and add to pool
    /// </summary>
    public void ReturnDiskReader(IDiskStream reader)
    {
        _readers.Enqueue(reader);
    }

    /// <summary>
    /// Read all allocation map pages. Allocation map pages contains initial position and fixed interval between other pages
    /// </summary>
    public async IAsyncEnumerable<PageBuffer> ReadAllocationMapPages()
    {
        uint positionID = AM_FIRST_PAGE_ID;

        var lastPositionID = this.GetLastFilePositionID();

        while (positionID <= lastPositionID)
        {
            var page = _bufferFactory.AllocateNewPage(false);

            await _writer.ReadPageAsync(positionID, page);

            if (page.IsHeaderEmpty())
            {
                _bufferFactory.DeallocatePage(page);
                break;
            }

            yield return page;

            positionID += AM_PAGE_STEP;
        }
    }

    /// <summary>
    /// </summary>
    public async Task WriteLogPagesAsync(PageBuffer[] pages)
    {
        //TODO: disk lock here

        // set recovery flag in header file to true at first use
        if (_writeInit == false)
        {
            _writeInit = true;
            _writer.WriteFlag(FileHeader.P_RECOVERY, 1);
        }

        for (var i = 0; i < pages.Length; i++)
        {
            var page = pages[i];

            ENSURE(page.PositionID == uint.MaxValue, $"current page {page.PositionID} should be MaxValue");

            // get next page position on log
            page.PositionID = this.GetNextLogPositionID();

            // write page to writer stream
            await _writer.WritePageAsync(page);
        }

        // flush to disk
        await _writer.FlushAsync();
    }

    /// <summary>
    /// Get next positionID in log
    /// </summary>
    public uint GetNextLogPositionID()
    {
        if (_logStartPositionID == 0 && _logEndPositionID == 0)
        {
            _logStartPositionID = this.GetLastFilePositionID() + 5; //TODO: calcular para proxima extend

            _logEndPositionID = (int)_logStartPositionID;

            return _logStartPositionID;
        }

        return (uint)Interlocked.Increment(ref _logEndPositionID);
    }

    /// <summary>
    /// Calculate, using disk file length, last PositionID. Should considering FILE_HEADER_SIZE and celling pages.
    /// </summary>
    public uint GetLastFilePositionID()
    {
        var fileLength = _streamFactory.GetLength();

        // fileLength must be, at least, FILE_HEADER
        if (fileLength <= FILE_HEADER_SIZE) throw ERR($"Invalid datafile. Data file is too small (length = {fileLength}).");

        var content = fileLength - FILE_HEADER_SIZE;
        var celling = content % PAGE_SIZE > 0 ? 1 : 0;
        var result = (uint)(content / PAGE_SIZE);

        // if last page was not completed written, add missing bytes to complete

        return (uint)(result + celling - 1);
    }

    public void Dispose()
    {
        //TODO: implementar fechamento de todos os streams
        // desalocar header
    }
}
