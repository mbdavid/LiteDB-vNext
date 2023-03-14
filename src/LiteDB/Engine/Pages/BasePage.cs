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
    /// Page memory service reference used in write operation
    /// </summary>
    private readonly IMemoryCacheService? _memoryCache;

    #region Buffer Field Positions

    public const int P_PAGE_ID = 0;  // 00-03 [uint]
    public const int P_PAGE_TYPE = 4; // 04-04 [byte]

    private const int P_CRC8 = 31; // 1 byte (last byte in header)

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
    public bool IsDirty => _writeBuffer is not null;

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

        span[P_PAGE_ID..].WriteUInt32(this.PageID);
        span[P_PAGE_TYPE] = (byte)this.PageType;
    }

    /// <summary>
    /// Create BasePage instance based on buffer content
    /// </summary>
    public BasePage(PageBuffer readBuffer, IMemoryCacheService memoryCache)
    {
        _readBuffer = readBuffer;
        _writeBuffer = null;
        _memoryCache = memoryCache;

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

        // if _readBuffer are not used by anyone in cache (ShareCounter == 1 - only current thread), remove
        if (_memoryCache!.TryRemovePageFromCache(_readBuffer, 1))
        {
            _writeBuffer = _readBuffer;
        }
        else
        {
            // create a new page in memory
            _writeBuffer = _memoryCache!.AllocateNewPage();

            // copy content from clean buffer to write buffer (if exists)
            _readBuffer.AsSpan().CopyTo(_writeBuffer.Value.AsSpan());
        }
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
