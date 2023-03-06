namespace LiteDB.Engine;

/// <summary>
/// Base page implement minimal page layer width read buffers. Pages are not thread-safe
/// </summary>
internal class BasePage
{
    /// <summary>
    /// Read buffer from disk or cache. It's concurrent read free
    /// Will be Memory[byte].Empty if empty new page
    /// </summary>
    protected IMemoryOwner<byte> _readBuffer;

    /// <summary>
    /// Created only use first write operation
    /// </summary>
    protected IMemoryOwner<byte>? _writeBuffer;

    /// <summary>
    /// Memory factory to create writeBuffer when page changes
    /// </summary>
    private readonly IMemoryFactory? _memoryFactory;

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
    public BasePage(uint pageID, PageType pageType, IMemoryOwner<byte> writeBuffer)
    {
        _writeBuffer = writeBuffer;
        _readBuffer = _writeBuffer;

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
    public BasePage(IMemoryOwner<byte> readBuffer, IMemoryFactory memoryFactory)
    {
        _readBuffer = readBuffer;
        _memoryFactory = memoryFactory;
        _writeBuffer = null;

        var span = readBuffer.Memory.Span;

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

        // rent buffer
        _writeBuffer = _memoryFactory!.Rent();

        // copy content from clean buffer to write buffer (if exists)
        _readBuffer.Memory.CopyTo(_writeBuffer.Memory);
    }

    /// <summary>
    /// Returns updated write buffer
    /// </summary>
    public virtual IMemoryOwner<byte> GetBufferWrite()
    {
        ENSURE(this.IsDirty, $"PageID {this.PageID} has no change");

        if (_writeBuffer is null) throw new ArgumentNullException(nameof(_writeBuffer));

        return _writeBuffer;
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
