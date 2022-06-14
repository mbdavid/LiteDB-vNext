namespace LiteDB.Engine;

/// <summary>
/// Header page represent first page on datafile. Engine contains a single instance of HeaderPage and all changes
/// must be syncornized (using lock).
/// </summary>
internal class HeaderPage : BasePage
{
    /// <summary>
    /// Header info the validate that datafile is a LiteDB file (27 bytes)
    /// </summary>
    public const string HEADER_INFO = "** This is a LiteDB file **";

    /// <summary>
    /// Datafile specification version
    /// </summary>
    public const byte FILE_VERSION = 9;

    #region Buffer Field Positions

    public const int P_HEADER_INFO = 5;  // 5-32 (27 bytes)
    public const int P_FILE_VERSION = 33; // 33-33 (1 byte)
    private const int P_CREATION_TIME = 34; // 34-42 (8 bytes)
    private const int P_LAST_PAGE_ID = 43; // 43-47 (4 bytes)

    #endregion

    /// <summary>
    /// DateTime when database was created [8 bytes]
    /// </summary>
    public DateTime CreationTime { get; }

    /// <summary>
    /// Get last physical page ID created [4 bytes]
    /// </summary>
    public uint LastPageID { get; private set; } = uint.MaxValue;

    public HeaderPage(Memory<byte> buffer, uint pageID)
        : base(buffer, pageID, PageType.Header)
    {
        // initialize page version
        this.CreationTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Load HeaderPage from buffer page
    /// </summary>
    public HeaderPage(Memory<byte> buffer)
        : base(buffer)
    {
        var span = buffer.Span;

        // read info and file version
        var info = span.ReadString(P_HEADER_INFO, HEADER_INFO.Length);
        var ver = span.ReadByte(P_FILE_VERSION);

        if (string.CompareOrdinal(info, HEADER_INFO) != 0 || ver != FILE_VERSION)
        {
            throw ERR_INVALID_DATABASE();
        }

        this.CreationTime = span.ReadDateTime(P_CREATION_TIME);
        this.LastPageID = span.ReadUInt32(P_LAST_PAGE_ID);
    }

    public override Memory<byte> GetBufferWrite()
    {
        var buffer = base.GetBufferWrite();
        var span = buffer.Span;

        // update header
        span.Write(HEADER_INFO, P_HEADER_INFO);
        span.Write(FILE_VERSION, P_FILE_VERSION);
        span.Write(this.CreationTime, P_CREATION_TIME);
        span.Write(this.LastPageID, P_LAST_PAGE_ID);

        return buffer;
    }
}
