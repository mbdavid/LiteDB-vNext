namespace LiteDB.Engine;

/// <summary>
/// Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
[Obsolete]
internal class __DiskService : I__DiskService
{
    // dependency injection
    private readonly IStreamFactory _streamFactory;
    private readonly I__ServicesFactory _factory;

    private readonly I__DiskStream _writer;

    private readonly ConcurrentQueue<I__DiskStream> _readers = new ();

    public I__DiskStream GetDiskWriter() => _writer;

    public __DiskService(
        IStreamFactory streamFactory,
        I__ServicesFactory factory)
    {
        _streamFactory = streamFactory;
        _factory = factory;

        _writer = factory.__CreateDiskStream();
    }

    /// <summary>
    /// Open (or create) datafile.
    /// </summary>
    public async ValueTask<FileHeader> InitializeAsync()
    {
        // if file not exists, create empty database
        if (_streamFactory.Exists() == false)
        {
            // intialize new database class factory
            var newFile = _factory.__CreateNewDatafile();

            // create first AM page and $master 
            return await newFile.CreateAsync(_writer);
        }
        else
        {
            // read header page buffer from start of disk
            return await _writer.OpenAsync(true);
        }
    }

    /// <summary>
    /// Rent a disk reader from pool. Must return after use
    /// </summary>
    public I__DiskStream RentDiskReader()
    {
        if (_readers.TryDequeue(out var reader))
        {
            return reader;
        }

        // create new diskstream
        reader = _factory.__CreateDiskStream();

        // and open to read-only (use saved header)
        reader.Open(_factory.FileHeader);

        return reader;
    }

    /// <summary>
    /// Return a rented reader and add to pool
    /// </summary>
    public void ReturnDiskReader(I__DiskStream reader)
    {
        _readers.Enqueue(reader);
    }

    public override string ToString()
    {
        return Dump.Object(new { _readers });
    }

    public void Dispose()
    {
        // dispose all open streams
        _writer.Dispose();

        foreach (var reader in _readers)
        {
            reader.Dispose();
        }

        // empty stream pool
        _readers.Clear();
    }
}
