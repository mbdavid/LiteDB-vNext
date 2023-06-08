namespace LiteDB.Engine;

/// <summary>
/// Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class DiskService : IDiskService
{
    // dependency injection
    private readonly IServicesFactory _factory;
    private readonly IStreamFactory _streamFactory;
    private readonly ILogService _logService;

    private readonly IDiskStream _writer;
    private FileHeader _fileHeader = new();

    private readonly ConcurrentQueue<IDiskStream> _readers = new ();

    public IDiskStream GetDiskWriter() => _writer;

    public FileHeader FileHeader => _fileHeader;

    public DiskService(
        IStreamFactory streamFactory,
        ILogService logService,
        IServicesFactory factory)
    {
        _streamFactory = streamFactory;
        _logService = logService;
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
            _fileHeader = await newFile.CreateAsync(_writer);
        }
        else
        {
            // read header page buffer from start of disk
            _fileHeader = await _writer.OpenAsync(true);
        }

        return _fileHeader;
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

        // create new diskstream
        reader = _factory.CreateDiskStream();

        // and open to read-only (use saved header)
        reader.Open(_fileHeader);

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
    /// </summary>
    public async Task WriteLogPagesAsync(PageBuffer[] pages)
    {
        //TODO: disk lock here

        // set recovery flag in header file to true at first use
        if (!_fileHeader.Recovery)
        {
            _fileHeader.Recovery = true;

            _writer.WriteFlag(FileHeader.P_RECOVERY, 1);
        }

        for (var i = 0; i < pages.Length; i++)
        {
            var page = pages[i];

            ENSURE(page.PositionID == int.MaxValue, $"current page {page.PositionID} should be MaxValue");

            // get next page position on log
            page.PositionID = _logService.GetNextLogPositionID();

            // write page to writer stream
            await _writer.WritePageAsync(page);

            // add page header only into log memory list
            _logService.AddLogPage(page.Header);
        }

        // flush to disk
        await _writer.FlushAsync();
    }

    /// <summary>
    /// Calculate, using disk file length, last PositionID. Should considering FILE_HEADER_SIZE and celling pages.
    /// </summary>
    public int GetLastFilePositionID()
    {
        var fileLength = _streamFactory.GetLength();

        // fileLength must be, at least, FILE_HEADER
        if (fileLength <= FILE_HEADER_SIZE) throw ERR($"Invalid datafile. Data file is too small (length = {fileLength}).");

        var content = fileLength - FILE_HEADER_SIZE;
        var celling = content % PAGE_SIZE > 0 ? 1 : 0;
        var result = content / PAGE_SIZE;

        // if last page was not completed written, add missing bytes to complete

        return (int)(result + celling - 1);
    }

    public void Dispose()
    {
        // if file was changed, update file header check byte
        if (_fileHeader.Recovery)
        {
            _writer.WriteFlag(FileHeader.P_RECOVERY, 0);

            // update file header
            _fileHeader.Recovery = false;

            _writer.Dispose();
        }

        foreach(var reader in _readers)
        {
            reader.Dispose();
        }
    }
}
