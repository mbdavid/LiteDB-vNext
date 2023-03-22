namespace LiteDB.Engine;

internal class PragmaDocument
{
    /// <summary>
    /// Internal user version control to detect database changes
    /// </summary>
    public int UserVersion { get; private set; } = 0;

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
    /// Initialize new pragma document
    /// </summary>
    public PragmaDocument()
    {
    }

    /// <summary>
    /// Read pragma information from a BsonDocument
    /// </summary>
    public PragmaDocument(BsonDocument doc)
    {
        this.UserVersion = doc[MK_PRAGMA_USER_VERSION];
        this.LimitSize = doc[MK_PRAGMA_USER_VERSION];
        this.Checkpoint = doc[MK_PRAGMA_USER_VERSION];
    }
}

