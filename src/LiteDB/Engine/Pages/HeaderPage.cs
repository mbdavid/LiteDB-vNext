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

    private const int P_CREATION_TIME = 5; // 5-13 (8 bytes)

    public const int P_CRC8 = 31; // 31-31 (1 byte)

    public const int P_HEADER_INFO = 32;  // 32-58 (27 bytes)
    public const int P_FILE_VERSION = 59; // 59-59 (1 byte)

    #endregion

    /// <summary>
    /// DateTime when database was created [8 bytes]
    /// </summary>
    public DateTime CreationTime { get; } = DateTime.UtcNow;

    /// <summary>
    /// Create new HeaderPage instance
    /// </summary>
    public HeaderPage(PageBuffer writeBuffer)
        : base(0, PageType.Header, writeBuffer)
    {
        var span = _writeBuffer!.Value.AsSpan();

        // update header
        span.WriteDateTime(this.CreationTime);

        // fixed content - can update buffer (header do not use shared cache)
        span[P_HEADER_INFO..].WriteString(HEADER_INFO);
        span[P_FILE_VERSION] = FILE_VERSION;
    }

    /// <summary>
    /// Load HeaderPage from buffer page
    /// </summary>
    public HeaderPage(PageBuffer readBuffer, IMemoryCacheService memoryCache) 
        : base(readBuffer, memoryCache)
    {
        var span = _readBuffer.AsSpan();

        // read header
        this.CreationTime = span[P_CREATION_TIME..8].ReadDateTime();

        // read content: info and file version
        var info = span[P_HEADER_INFO..HEADER_INFO.Length].ReadString();
        var ver = span[P_FILE_VERSION];

        if (string.CompareOrdinal(info, HEADER_INFO) != 0 || ver != FILE_VERSION)
        {
            throw ERR_INVALID_DATABASE();
        }
    }

    /// <summary>
    /// Update writer buffer with header variables changes
    /// </summary>
    public override PageBuffer UpdateHeaderBuffer()
    {
        var buffer = base.UpdateHeaderBuffer();
        var span = buffer.AsSpan();

        // update header
        span[P_LAST_PAGE_ID..].WriteUInt32(this.LastPageID);

        return buffer;
    }
}
