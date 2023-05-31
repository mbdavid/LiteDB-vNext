namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class DiskStream : IDiskStream
{
    private readonly IEngineSettings _settings;
    private readonly IStreamFactory _streamFactory;

    private Stream? _stream;
    private Stream? _contentStream;

    public string Name => Path.GetFileName(_settings.Filename);

    public DiskStream(IServicesFactory factory)
    {
        _settings = factory.Settings;
        _streamFactory = factory.GetStreamFactory();
    }

    /// <summary>
    /// Initialize disk opening already exist datafile and return file header structure.
    /// Can open file as read or write
    /// </summary>
    public async Task<FileHeader> OpenAsync(bool canWrite)
    {
        // get a new FileStream connected to file
        _stream = _streamFactory.GetStream(canWrite, 
            canWrite ? FileOptions.SequentialScan : FileOptions.RandomAccess);

        // reading file header
        var buffer = new byte[FILE_HEADER_SIZE];

        _stream.Position = 0;

        await _stream.ReadAsync(buffer);

        var header = new FileHeader(buffer);

        // for content stream, use AesStream (for encrypted file) or same _stream
        _contentStream = header.Encrypted ?
            new AesStream(_stream, _settings.Password ?? "", header.EncryptionSalt) :
            _stream;

        return header;
    }

    /// <summary>
    /// Initialize disk creating a new datafile and writing file header
    /// </summary>
    public async Task CreateAsync(FileHeader fileHeader)
    {
        // create new data file
        _stream = _streamFactory.GetStream(true, FileOptions.SequentialScan);

        // writing file header
        _stream.Position = 0;

        await _stream.WriteAsync(fileHeader.GetBuffer(), 0, FILE_HEADER_SIZE);

        // for content stream, use AesStream (for encrypted file) or same _stream
        _contentStream = fileHeader.Encrypted ?
            new AesStream(_stream, _settings.Password ?? "", fileHeader.EncryptionSalt) :
            _stream;
    }

    public Task FlushAsync()
    {
        return _contentStream?.FlushAsync() ?? Task.CompletedTask;
    }

    /// <summary>
    /// Read single page from disk using disk position. This position has FILE_HEADER_SIZE offset
    /// </summary>
    public async Task<bool> ReadPageAsync(uint positionID, PageBuffer page)
    {
        if (_stream is null || _contentStream is null) throw new InvalidOperationException("Datafile closed");

        // set real position on stream
        _contentStream.Position = FILE_HEADER_SIZE + (positionID * PAGE_SIZE);

        var read = await _contentStream.ReadAsync(page.Buffer, 0, PAGE_SIZE);

        // after read content from file, update header info in page
        page.Header.ReadFromPage(page);

        // update page position
        page.PositionID = positionID;

        return read == PAGE_SIZE;
    }

    public async Task WritePageAsync(PageBuffer page)
    {
        if (_stream is null || _contentStream is null) throw new InvalidOperationException("Datafile closed");

        ENSURE(page.PositionID != uint.MaxValue, "PageBuffer must have defined Position before WriteAsync");

        // before save on disk, update header page to buffer (first 32 bytes)
        page.Header.WriteToPage(page);

        // set real position on stream
        _contentStream.Position = FILE_HEADER_SIZE + (page.PositionID * PAGE_SIZE);

        await _contentStream.WriteAsync(page.Buffer, 0, PAGE_SIZE);
    }

    /// <summary>
    /// Write a specific byte in datafile with a flag/byte value - used to restore. Use sync write
    /// </summary>
    public void WriteFlag(int headerPosition, byte flag)
    {
        if (_stream is null) throw new InvalidOperationException("Datafile closed");

        _stream.Position = headerPosition;
        _stream.WriteByte(flag);

        _stream.Flush();
    }

    public void Dispose()
    {
        _contentStream?.Dispose();
    }
}
