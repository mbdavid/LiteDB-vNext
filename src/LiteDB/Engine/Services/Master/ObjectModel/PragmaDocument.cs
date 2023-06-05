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
    public int LimitSizeID { get; private set; } = 0;

    /// <summary>
    /// When LOG file gets larger than checkpoint size (in pages), do a soft checkpoint (and also do a checkpoint at shutdown)
    /// Checkpoint = 0 means there's no auto-checkpoint nor shutdown checkpoint
    /// </summary>
    public int Checkpoint { get; private set; } = 1000;

    /// <summary>
    /// Read pragma information from a BsonDocument
    /// </summary>
    public PragmaDocument(BsonDocument doc)
    {
        this.UserVersion = doc[MK_PRAGMA_USER_VERSION];
        this.LimitSizeID = doc[MK_PRAGMA_LIMIT_SIZE_ID];
        this.Checkpoint = doc[MK_PRAGMA_CHECKPOINT];
    }
}

