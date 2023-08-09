using System.IO;

namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class DiskStream : IDiskStream
{
    private readonly IEngineSettings _settings;
    private readonly IStreamFactory _streamFactory;

    private Stream? _stream;
    private Stream? _contentStream;

    public string Name => Path.GetFileName(_settings.Filename);

    public DiskStream(
        IEngineSettings settings, 
        IStreamFactory streamFactory)
    {
        _settings = settings;
        _streamFactory = streamFactory;
    }

    /// <summary>
    /// Initialize disk opening already exist datafile and return file header structure.
    /// Can open file as read or write
    /// </summary>
    public async ValueTask<FileHeader> OpenAsync(bool canWrite, CancellationToken ct = default)
    {
        // get a new FileStream connected to file
        _stream = _streamFactory.GetStream(canWrite,
            canWrite ? FileOptions.SequentialScan : FileOptions.RandomAccess);

        // reading file header
        var buffer = new byte[FILE_HEADER_SIZE];

        _stream.Position = 0;

        var read = await _stream.ReadAsync(buffer, ct);

        ENSURE(() => read != PAGE_HEADER_SIZE);

        var header = new FileHeader(buffer);

        // for content stream, use AesStream (for encrypted file) or same _stream
        _contentStream = header.Encrypted ?
            new AesStream(_stream, _settings.Password ?? "", header.EncryptionSalt) :
            _stream;

        return header;
    }

    /// <summary>
    /// Open stream with no FileHeader read
    /// </summary>
    public void Open(FileHeader header)
    {
        // get a new FileStream connected to file
        _stream = _streamFactory.GetStream(false, FileOptions.RandomAccess);

        // for content stream, use AesStream (for encrypted file) or same _stream
        _contentStream = header.Encrypted ?
            new AesStream(_stream, _settings.Password ?? "", header.EncryptionSalt) :
            _stream;
    }

    /// <summary>
    /// Initialize disk creating a new datafile and writing file header
    /// </summary>
    public async ValueTask CreateAsync(FileHeader fileHeader, CancellationToken ct = default)
    {
        // create new data file
        _stream = _streamFactory.GetStream(true, FileOptions.SequentialScan);

        // writing file header
        _stream.Position = 0;

        await _stream.WriteAsync(fileHeader.ToArray(), ct);

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

    /// <summary>
    /// Read single page from disk using disk position. This position has FILE_HEADER_SIZE offset
    /// </summary>
    public async ValueTask<bool> ReadPageAsync(int positionID, PageBuffer page, CancellationToken ct = default)
    {
        // set real position on stream
        _contentStream!.Position = FILE_HEADER_SIZE + (positionID * PAGE_SIZE);

        var read = await _contentStream.ReadAsync(page.Buffer, ct);

        // after read content from file, update header info in page
        page.Header.ReadFromPage(page);

        // update page position
        page.PositionID = positionID;

        return read == PAGE_SIZE;
    }

    public async ValueTask WritePageAsync(PageBuffer page, CancellationToken ct = default)
    {
        ENSURE(() => page.IsDirty);
        ENSURE(() => page.PositionID != int.MaxValue);

        // update crc8 page
        page.Header.Crc8 = page.ComputeCrc8();

        // before save on disk, update header page to buffer (first 32 bytes)
        page.Header.WriteToPage(page);

        // set real position on stream
        _contentStream!.Position = FILE_HEADER_SIZE + (page.PositionID * PAGE_SIZE);

        await _contentStream.WriteAsync(page.Buffer, ct);

        // clear isDirty flag
        page.IsDirty = false;
    }

    /// <summary>
    /// Write an empty (full \0) PAGE_SIZE using positionID
    /// </summary>
    public async ValueTask WriteEmptyAsync(int positionID, CancellationToken ct = default)
    {
        // set real position on stream
        _contentStream!.Position = FILE_HEADER_SIZE + (positionID * PAGE_SIZE);

        await _contentStream.WriteAsync(PAGE_EMPTY_BUFFER, ct);
    }

    /// <summary>
    /// Write an empty (full \0) PAGE_SIZE using from/to (inclusive)
    /// </summary>
    public async ValueTask WriteEmptyAsync(int fromPositionID, int toPositionID, CancellationToken ct = default)
    {
        for (var i = fromPositionID; i <= toPositionID; i++)
        {
            // set real position on stream
            _contentStream!.Position = FILE_HEADER_SIZE + (i * PAGE_SIZE);

            await _contentStream.WriteAsync(PAGE_EMPTY_BUFFER, ct);
        }
    }

    /// <summary>
    /// Set new file length using lastPageID as end of file.
    /// 0 = 8k, 1 = 16k, ...
    /// </summary>
    public void SetSize(int lastPageID)
    {
        var fileLength = FILE_HEADER_SIZE +
            ((lastPageID + 1) * PAGE_SIZE);

        _stream!.SetLength(fileLength);
    }

    /// <summary>
    /// Write a specific byte in datafile with a flag/byte value - used to restore. Use sync write
    /// </summary>
    public void WriteFlag(int headerPosition, byte flag)
    {
        _stream!.Position = headerPosition;
        _stream.WriteByte(flag);

        _stream.Flush();
    }

    /// <summary>
    /// Close stream (disconect from disk)
    /// </summary>
    public void Dispose()
    {
        _stream?.Dispose();
        _contentStream?.Dispose();
    }
}
