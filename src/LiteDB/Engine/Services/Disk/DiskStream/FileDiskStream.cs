namespace LiteDB.Engine;

internal class FileDiskStream : IDiskStream
{
    private readonly IEngineSettings _settings;

    private Stream? _stream;
    private Stream? _contentStream;

    public string Name => Path.GetFileName(_settings.Filename);

    public FileDiskStream(IEngineSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Get filelength from descriptor
    /// </summary>
    public long GetLength()
    {
        // if file don't exists, returns 0
        if (!this.Exists()) return 0;

        // get physical file length from OS
        var length = new FileInfo(_settings.Filename).Length;

        return length;
    }

    /// <summary>
    /// Check if file exists without create/open
    /// </summary>
    public bool Exists()
    {
        return File.Exists(_settings.Filename);
    }

    /// <summary>
    /// Delete current file
    /// </summary>
    public void Delete()
    {
        File.Delete(_settings.Filename);
    }

    /// <summary>
    /// Initialize disk opening already exist datafile and return file header structure.
    /// Can open file as read or write
    /// </summary>
    public async Task<FileHeader> OpenAsync()
    {
        // open stream
        _stream = new FileStream(
            _settings.Filename,
            FileMode.Open,
            _settings.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
            _settings.ReadOnly ? FileShare.ReadWrite : FileShare.Read,
            PAGE_SIZE,
            FileOptions.RandomAccess);

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
        _stream = new FileStream(
            _settings.Filename,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.Read,
            PAGE_SIZE,
            FileOptions.RandomAccess);

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
    public async Task<bool> ReadPageAsync(long position, PageBuffer buffer)
    {
        if (_contentStream is null) throw new InvalidOperationException("Datafile closed");

        // add header file offset
        _contentStream.Position = position + FILE_HEADER_SIZE;

        var read = await _contentStream.ReadAsync(buffer.Buffer, 0, PAGE_SIZE);

        buffer.ReadHeader();

        return read == PAGE_SIZE;
    }

    public async Task WritePageAsync(PageBuffer buffer)
    {
        if (_contentStream is null) throw new InvalidOperationException("Datafile closed");

        ENSURE(buffer.Position != long.MaxValue, "PageBuffer must have defined Position before WriteAsync");

        // add header file offset
        _contentStream.Position = buffer.Position + FILE_HEADER_SIZE;

        // before store on this, update header page (first 32 bytes)
        buffer.WriteHeader();

        await _contentStream.WriteAsync(buffer.Buffer, 0, PAGE_SIZE);
    }

    public void Dispose()
    {
        _contentStream?.Dispose();
    }
}
