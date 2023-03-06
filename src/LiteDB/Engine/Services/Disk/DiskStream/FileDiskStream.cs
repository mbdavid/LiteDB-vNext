namespace LiteDB.Engine;

internal class FileDiskStream : IDiskStream
{
    private readonly string _filename;
    private readonly string? _password;
    private readonly bool _readonly;
    private readonly bool _sequencial;
    private Stream? _stream;

    public string Name => Path.GetFileName(_filename);

    public FileDiskStream(string filename, string? password, bool readOnly, bool sequencial)
    {
        _filename = filename;
        _password = password;
        _readonly = readOnly;
        _sequencial = sequencial;
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
            _sequencial ? FileOptions.SequentialScan : FileOptions.RandomAccess);

        //if (stream.Length == 0 && _hidden)
        //{
        //    File.SetAttributes(_filename, FileAttributes.Hidden);
        //}

        // le header/salt se _password!=null

        //return _password == null ? (Stream)stream : new AesStream(_password, _salt, stream);
        return stream;
    }

    public Task FlushAsync()
    {
        return _stream?.FlushAsync() ?? Task.CompletedTask;
    }

    public async Task<bool> ReadAsync(long position, Memory<byte> buffer)
    {
        _stream ??= await CreateStreamAsync();

        var read = await _stream.ReadAsync(buffer);

        return read == PAGE_SIZE;
    }

    public async Task WriteAsync(long position, Memory<byte> buffer)
    {
        _stream ??= await CreateStreamAsync();

        _stream.Position = position;

        await _stream.WriteAsync(buffer);
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }
}
