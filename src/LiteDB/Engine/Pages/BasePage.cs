namespace LiteDB.Engine;

/// <summary>
/// Base page implement minimal page layer width read buffers
/// </summary>
internal class BasePage : IDisposable
{
    /// <summary>
    /// Read buffer from disk or cache. It's concurrent read free
    /// Will be Memory[byte].Empty if empty new page
    /// </summary>
    protected IMemoryOwner<byte> _readBuffer;

    /// <summary>
    /// Created only use first write operation
    /// Changes on BasePage must be one same thread (Not Thread Safe). Only one writer per time
    /// </summary>
    protected IMemoryOwner<byte> _writeBuffer;

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
    public BasePage(uint pageID, PageType pageType)
    {
        _readBuffer = new BufferPage(true);
        _writeBuffer = _readBuffer;

        // initialize
        this.PageID = pageID;
        this.PageType = pageType;

        // write fixed data
        var span = _readBuffer.Memory.Span;

        span[P_PAGE_ID..].WriteUInt32(this.PageID);
        span[P_PAGE_TYPE] = (byte)this.PageType;
    }

    /// <summary>
    /// Create BasePage instance based on buffer content
    /// </summary>
    public BasePage(IMemoryOwner<byte> buffer)
    {
        _readBuffer = buffer;
        _writeBuffer = null;

        var span = buffer.Memory.Span;

        this.PageID = span[P_PAGE_ID..].ReadUInt32();
        this.PageType = (PageType)span[P_PAGE_TYPE];
    }

    #region Write Operations

    /// <summary>
    /// Initialize _writeBuffer on first write use
    /// </summary>
    protected virtual void InitializeWrite()
    {
        if (_writeBuffer != null) return;

        // rent buffer
        _writeBuffer = new BufferPage(false);

        // copy content from clean buffer to write buffer (if exists)
        _readBuffer.Memory.CopyTo(_writeBuffer.Memory);
    }

    /// <summary>
    /// Returns updated write buffer
    /// </summary>
    public virtual Memory<byte> GetBufferWrite()
    {
        if (this.IsDirty == false) throw new InvalidOperationException("Current page has no dirty buffer");

        return _writeBuffer.Memory;
    }

    /// <summary>
    /// Dispose both
    /// </summary>
    public void Dispose()
    {
        if (_readBuffer == _writeBuffer)
        {
            _readBuffer?.Dispose();
        }

        _readBuffer.Dispose();
        _writeBuffer?.Dispose();
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
