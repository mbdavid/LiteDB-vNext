namespace LiteDB.Engine;

internal class FileDiskStream : IDiskStream
{
    private readonly string _filename;
    private readonly string? _password;
    private readonly bool _readonly;
    private readonly bool _sequential;
    private Stream? _stream;

    public string Name => Path.GetFileName(_filename);

    public FileDiskStream(string filename, string? password, bool readOnly, bool sequencial)
    {
        _filename = filename;
        _password = password;
        _readonly = readOnly;
        _sequential = sequencial;
    }

    /// <summary>
    /// Get filelength from descriptor
    /// </summary>
    public long GetLength()
    {
        // if file don't exists, returns 0
        if (!this.Exists()) return 0;

        // get physical file length from OS
        var length = new FileInfo(_filename).Length;

        return length;
    }

    /// <summary>
    /// Check if file exists without create/open
    /// </summary>
    public bool Exists()
    {
        return File.Exists(_filename);
    }

    /// <summary>
    /// Delete current file
    /// </summary>
    public void Delete()
    {
        File.Delete(_filename);
    }

    private async Task<Stream> CreateStreamAsync()
    {
        var stream = new FileStream(
            _filename,
            _readonly ? FileMode.Open : FileMode.OpenOrCreate,
            _readonly ? FileAccess.Read : FileAccess.ReadWrite,
            _readonly ? FileShare.ReadWrite : FileShare.Read,
            PAGE_SIZE,
            _sequential ? FileOptions.SequentialScan : FileOptions.RandomAccess);

        //if (stream.Length == 0 && _hidden)
        //{
        //    File.SetAttributes(_filename, FileAttributes.Hidden);
        //}

        if (_password is not null)
        {
            var headerBuffer = ArrayPool<byte>.Shared.Rent(FILE_HEADER_SIZE);

            await stream.ReadAsync(headerBuffer, 0, FILE_HEADER_SIZE);

            var isEncrypted = headerBuffer[FileHeader.P_ENCRYPTED];
            var salt = headerBuffer[FileHeader.P_ENCRYPTION_SALT..(FileHeader.P_ENCRYPTION_SALT + ENCRYPTION_SALT_SIZE)];

        }

        // le header/salt se _password!=null

        //return _password == null ? (Stream)stream : new AesStream(_password, _salt, stream);
        return stream;
    }

    public Task FlushAsync()
    {
        return _stream?.FlushAsync() ?? Task.CompletedTask;
    }

    public async Task<bool> ReadPageAsync(long position, PageBuffer buffer)
    {
        _stream ??= await CreateStreamAsync();

        // add header file offset
        _stream.Position = position + FILE_HEADER_SIZE;

        var read = await _stream.ReadAsync(buffer.Array, 0, PAGE_SIZE);

        return read == PAGE_SIZE;
    }

    public async Task WritePageAsync(PageBuffer buffer)
    {
        ENSURE(buffer.Position != long.MaxValue, "PageBuffer must have defined Position before WriteAsync");

        _stream ??= await CreateStreamAsync();

        // add header file offset
        _stream.Position = buffer.Position + FILE_HEADER_SIZE;

        await _stream.WriteAsync(buffer.Array, 0, PAGE_SIZE);
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }
}
