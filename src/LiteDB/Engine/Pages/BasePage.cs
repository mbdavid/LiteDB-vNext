namespace LiteDB.Engine;

/// <summary>
/// Base page implement minimal page layer width read buffers. Pages are not thread-safe
/// * Shared (thread safe) (N readers, 1 writer)
/// </summary>
internal class BasePage
{
    /// <summary>
    /// Read buffer from disk or cache. It's concurrent read free
    /// Will be Memory[byte].Empty if empty new page
    /// </summary>
    protected PageBuffer _readBuffer;

    /// <summary>
    /// Created only use first write operation
    /// </summary>
    protected PageBuffer? _writeBuffer;

    /// <summary>
    /// Function to create writable buffer
    /// </summary>
    private readonly IPageWriteFactoryService? _writeFactory;

    #region Buffer Field Positions

    public const int P_PAGE_ID = 0;  // 00-03 [uint]
    public const int P_PAGE_TYPE = 4; // 04-04 [byte]
    // 05-30 (26 bytes reserved for other page types)
    public const int P_CRC8 = 31; // 1 byte (last byte in header)

    #endregion

    /// <summary>
    /// Represent page number - start in 0 with HeaderPage [4 bytes]
    /// </summary>
    public uint PageID { get; }

    /// <summary>
    /// Indicate the page type [1 byte]
    /// </summary>
    public PageType PageType { get; set; }

    /// <summary>
    /// If true any change operation InitializeWrite()
    /// </summary>
    public virtual bool IsDirty => _writeBuffer is not null;

    /// <summary>
    /// Create a new BasePage with an empty buffer. Write PageID and PageType on buffer
    /// </summary>
    public BasePage(uint pageID, PageType pageType, PageBuffer writeBuffer)
    {
        _writeBuffer = writeBuffer;
        _readBuffer = writeBuffer;

        // initialize
        this.PageID = pageID;
        this.PageType = pageType;

        // write fixed data
        var span = _readBuffer.AsSpan();

        // clear write buffer to ensure all content will be 0 (empty)
        span.Fill(0);

        span[P_PAGE_ID..].WriteUInt32(this.PageID);
        span[P_PAGE_TYPE] = (byte)this.PageType;
    }

    /// <summary>
    /// Create BasePage instance based on buffer content
    /// </summary>
    public BasePage(PageBuffer readBuffer, IPageWriteFactoryService? writeFactory)
    {
        _readBuffer = readBuffer;
        _writeFactory = writeFactory;

        // if has no write factory, use same read buffer to write
        _writeBuffer = writeFactory is null ? _readBuffer : null;

        var span = readBuffer.AsSpan();

        this.PageID = span[P_PAGE_ID..].ReadUInt32();
        this.PageType = (PageType)span[P_PAGE_TYPE];
    }

    #region Write Operations

    /// <summary>
    /// Initialize _writeBuffer on first write use
    /// </summary>
    protected virtual void InitializeWrite()
    {
        if (_writeBuffer is not null) return;

        // create a new write buffer in memory or re-use if this buffer are not used in cache
        _writeBuffer = _writeFactory!.GetWriteBuffer(_readBuffer);
    }

    /// <summary>
    /// Update writer buffer with header variables changes
    /// </summary>
    public virtual PageBuffer UpdateHeaderBuffer()
    {
        ENSURE(this.IsDirty, $"PageID {this.PageID} has no change");

        if (_writeBuffer.HasValue == false) throw new ArgumentNullException(nameof(_writeBuffer));

        return _writeBuffer.Value;
    }

    #endregion

    #region Static Helpers

    /// <summary>
    /// Returns a size of specified number of pages
    /// </summary>
    public static long GetPagePosition(uint pageID)
    {
        return checked((long)pageID * PAGE_SIZE);
    }

    /// <summary>
    /// Returns a size of specified number of pages
    /// </summary>
    public static long GetPagePosition(int pageID)
    {
        return GetPagePosition((uint)pageID);
    }

    #endregion

}
