namespace LiteDB.Engine;

internal class FileStreamFactory : IStreamFactory
{
    private readonly IEngineSettings _settings;

    public string Name => Path.GetFileName(_settings.Filename);

    public FileStreamFactory(IEngineSettings settings)
    {
        _settings = settings;
    }

    public void Delete()
    {
        File.Delete(_settings.Filename);
    }

    public bool Exists()
    {
        return File.Exists(_settings.Filename);
    }

    public long GetLength()
    {
        // if file don't exists, returns 0
        if (!this.Exists()) return 0;

        // get physical file length from OS
        var length = new FileInfo(_settings.Filename).Length;

        return length;
    }

    /// <summary>
    /// Create new file and return FileStream implementation
    /// </summary>
    public Stream Create(FileOptions options)
    {
        // create new data file
        return new FileStream(
            _settings.Filename,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.Read,
            PAGE_SIZE,
            options);
    }

    /// <summary>
    /// Open an existing data file and return FileStream implementation
    /// </summary>
    public Stream GetStream(bool canWrite, FileOptions options)
    {
        var write = canWrite && (_settings.ReadOnly == false);

        var stream = new FileStream(
            _settings.Filename,
            _settings.ReadOnly ? FileMode.Open : FileMode.OpenOrCreate,
            write ? FileAccess.ReadWrite : FileAccess.Read,
            write ? FileShare.Read : FileShare.ReadWrite,
            PAGE_SIZE,
            options);

        return stream;
    }
}
