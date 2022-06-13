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

    private const int P_PRAGMAS = 32; // 32-8191 (?? bytes)

    #endregion

    /// <summary>
    /// DateTime when database was created [8 bytes]
    /// </summary>
    public DateTime CreationTime { get; }

    public HeaderPage(Memory<byte> buffer, uint pageID)
        : base(buffer, pageID, PageType.Header)
    {
        var span = buffer.Span;

        // initialize page version
        this.CreationTime = DateTime.UtcNow;

        // initialize pragmas
        //this.Pragmas = new EnginePragmas(this);

        // writing direct into buffer in Ctor() because there is no change later (write once)
        span.Write(HEADER_INFO, P_HEADER_INFO);
        span.Write(FILE_VERSION, P_FILE_VERSION);
        span.Write(this.CreationTime, P_CREATION_TIME);
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
    }
}
