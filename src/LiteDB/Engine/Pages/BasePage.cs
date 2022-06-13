namespace LiteDB.Engine;

/// <summary>
/// Base page implement minimal page layer width read buffers
/// </summary>
internal class BasePage
{
    protected Memory<byte> _buffer;

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
    /// If true, this page contains data changes on buffer (page must saved)
    /// </summary>
    public virtual bool IsDirty { get; } = false;

    /// <summary>
    /// Create a new BasePage with an empty buffer. Write PageID and PageType on buffer
    /// </summary>
    public BasePage(Memory<byte> buffer, uint pageID, PageType pageType)
    {
        _buffer = buffer;

        var span = buffer.Span;

        // initialize
        this.PageID = pageID;
        this.PageType = pageType;

        // writing direct into buffer in Ctor() because there is no change later (write once)
        span.Write(this.PageID, P_PAGE_ID);
        span.Write((byte)this.PageType, P_PAGE_TYPE);
    }

    /// <summary>
    /// Create BasePage instance based on buffer content
    /// </summary>
    public BasePage(Memory<byte> buffer)
    {
        _buffer = buffer;

        var span = buffer.Span;

        this.PageID = span.ReadUInt32(P_PAGE_ID);
        this.PageType = (PageType)span.ReadByte(P_PAGE_TYPE);
    }

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
