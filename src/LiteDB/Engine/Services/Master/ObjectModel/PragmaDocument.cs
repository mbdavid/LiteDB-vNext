namespace LiteDB.Engine;

internal class PragmaDocument
{
    /// <summary>
    /// Internal user version control to detect database changes
    /// </summary>
    public int UserVersion { get; private set; } = 0;

    /// <summary>
    /// Define collation for this database. Value will be persisted on disk at first write database. After this, there is no change of collation
    /// (read only - created on new datafile)
    /// </summary>
    public Collation Collation { get; }

    /// <summary>
    /// Timeout for waiting unlock operations (default: 1 minute)
    /// </summary>
    public TimeSpan Timeout { get; private set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Max limit of datafile (in bytes) (default: MaxValue)
    /// </summary>
    public long LimitSize { get; private set; } = long.MaxValue;

    /// <summary>
    /// When LOG file gets larger than checkpoint size (in pages), do a soft checkpoint (and also do a checkpoint at shutdown)
    /// Checkpoint = 0 means there's no auto-checkpoint nor shutdown checkpoint
    /// </summary>
    public int Checkpoint { get; private set; } = 1000;


    /// <summary>
    /// Initialize new pragma document (based only in collection info)
    /// </summary>
    public PragmaDocument(Collation collation)
    {
        this.Collation = collation;
    }

    /// <summary>
    /// Read pragma information from a BsonDocument
    /// </summary>
    public PragmaDocument(BsonDocument doc)
    {
        this.UserVersion = doc[MK_PRAGMA_USER_VERSION];
        this.Collation = new Collation(doc[MK_PRAGMA_USER_VERSION].AsString);
        this.Timeout = TimeSpan.FromSeconds(doc[MK_PRAGMA_USER_VERSION].AsInt32);
        this.LimitSize = doc[MK_PRAGMA_USER_VERSION];
        this.Checkpoint = doc[MK_PRAGMA_USER_VERSION];
    }
}

