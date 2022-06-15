namespace LiteDB.Engine;

/// <summary>
/// Base page implement minimal page layer width read buffers
/// </summary>
internal class BasePage : IDisposable
{
    /// <summary>
    /// Memory cache service to create write memory pages before write on disk
    /// </summary>
    protected readonly MemoryCache _cache;

    /// <summary>
    /// Read buffer from disk or cache. It's concurrent read free
    /// Will be Memory[byte].Empty if empty new page
    /// </summary>
    protected Memory<byte> _readBuffer;

    /// <summary>
    /// Created only use first write operation
    /// Changes on BasePage must be one same thread (Not Thread Safe). Only one writer per time
    /// </summary>
    protected MemoryCachePage _writeBuffer;

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
    public bool IsDirty => _writeBuffer != null;

    /// <summary>
    /// Create a new BasePage with an empty buffer. Write PageID and PageType on buffer
    /// </summary>
    public BasePage(MemoryCache cache, uint pageID, PageType pageType)
    {
        _cache = cache;
        _readBuffer = Memory<byte>.Empty;

        // initialize
        this.PageID = pageID;
        this.PageType = pageType;
    }

    /// <summary>
    /// Create BasePage instance based on buffer content
    /// </summary>
    public BasePage(MemoryCache cache, Memory<byte> buffer)
    {
        _cache = cache;
        _readBuffer = buffer;

        var span = buffer.Span;

        this.PageID = span.ReadUInt32(P_PAGE_ID);
        this.PageType = (PageType)span.ReadByte(P_PAGE_TYPE);
    }

    #region Write Operations

    /// <summary>
    /// Initialize _writeBuffer on first write use
    /// </summary>
    protected virtual void InitializeWrite()
    {
        if (_writeBuffer != null) return;

        // rent buffer
        _writeBuffer = _cache.NewPage();

        // copy content from clean buffer to write buffer (if exists)
        if (!_readBuffer.IsEmpty)
        {
            _readBuffer.CopyTo(_writeBuffer.Buffer);
        }
    }

    /// <summary>
    /// Returns updated write buffer
    /// </summary>
    public virtual Memory<byte> GetBufferWrite()
    {
        if (this.IsDirty == false) throw new InvalidOperationException("Current page has no dirty buffer");

        var buffer = _writeBuffer.Buffer;
        var span = buffer.Span;

        // writing direct into buffer in Ctor() because there is no change later (write once)
        span.Write(this.PageID, P_PAGE_ID);
        span.Write((byte)this.PageType, P_PAGE_TYPE);

        return buffer;
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

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    #endregion
}
