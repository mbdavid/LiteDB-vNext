using System.Runtime;

namespace LiteDB.Engine;

[AutoInterface]
internal class FileDisk : IFileDisk
{
    private readonly IEngineSettings _settings;
    private readonly IStreamFactory _streamFactory;

    private Stream? _stream;
    private Stream? _contentStream;

    public string Name => Path.GetFileName(_settings.Filename);

    public FileDisk(IServicesFactory factory)
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
            canWrite ? FileOptions.RandomAccess : FileOptions.SequentialScan);

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
    public async Task<bool> ReadPageAsync(long position, PageBuffer page)
    {
        if (_contentStream is null) throw new InvalidOperationException("Datafile closed");

        // add header file offset
        _contentStream.Position = position + FILE_HEADER_SIZE;

        var read = await _contentStream.ReadAsync(page.Buffer, 0, PAGE_SIZE);

        // after read content from file, update header info in page
        page.Header.ReadFromBuffer(page.Buffer);

        // update page position
        page.Position = position;

        return read == PAGE_SIZE;
    }

    public async Task WritePageAsync(PageBuffer page)
    {
        if (_contentStream is null) throw new InvalidOperationException("Datafile closed");

        ENSURE(page.Position != long.MaxValue, "PageBuffer must have defined Position before WriteAsync");

        // add header file offset
        _contentStream.Position = page.Position + FILE_HEADER_SIZE;

        // before save on disk, update header page to buffer (first 32 bytes)
        page.Header.WriteToBuffer(page.Buffer);

        await _contentStream.WriteAsync(page.Buffer, 0, PAGE_SIZE);
    }

    public void Dispose()
    {
        _contentStream?.Dispose();
    }
}
