namespace LiteDB.Engine;

/// <summary>
/// All engine settings used to starts new engine
/// </summary>
public class EngineSettings
{
    private readonly IDictionary<string, string> _settings;

    public EngineSettings()
    {
        _settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public EngineSettings(IDictionary<string, string> settings)
    {
        _settings = settings;
        this.Filename = settings["filename"];
        this.Password = settings["password"];
        //this.InitialSize = settings[]
    }

    /// <summary>
    /// Get a key/value from parsed from connection string. Returns null if not found. Used for plugins
    /// </summary>
    public string this[string key]
    {
        get => _settings.GetOrDefault(key);
        set => _settings[key] = value;
    }

    /// <summary>
    /// Get/Set custom stream to be used as datafile (can be MemoryStrem or TempStream). Do not use FileStream - to use physical file, use "filename" attribute (and keep DataStrem null)
    /// </summary>
    public Stream DataStream { get; set; }

    /// <summary>
    /// Full path or relative path from DLL directory. Can use ':temp:' for temp database or ':memory:' for in-memory database. (default: null)
    /// </summary>
    public string Filename { get; set; }

    /// <summary>
    /// Get database password to decrypt pages
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// If database is new, initialize with allocated space (in bytes) (default: 0)
    /// </summary>
    public long InitialSize { get; set; } = 0;

    /// <summary>
    /// Create database with custom string collection (used only to create database) (default: Collation.Default)
    /// </summary>
    public Collation Collation { get; set; }

    /// <summary>
    /// Indicate that engine will open files in readonly mode (and will not support any database change)
    /// </summary>
    public bool ReadOnly { get; set; } = false;
}
