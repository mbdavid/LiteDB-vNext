namespace LiteDB.Engine;

internal enum PageType { Empty = 0, Header = 1, AllocationMap = 2, Index = 3, Data = 4 }

internal class BasePage
{
    protected readonly Memory<byte> _buffer;

    #region Page Header Positions

    public const int P_PAGE_ID = 0;  // 00-03 [uint]
    public const int P_PAGE_TYPE = 4; // 04-04 [byte]

    public const int P_CRC8 = 31; // 31-31 [byte]

    #endregion

    /// <summary>
    /// Represent page number - start in 0 with HeaderPage [4 bytes]
    /// </summary>
    public uint PageID { get; }

    /// <summary>
    /// Indicate the page type [1 byte]
    /// </summary>
    public PageType PageType { get; private set; }

    /// <summary>
    /// Set this pages that was changed and must be persist in disk [not peristable]
    /// </summary>
    public bool IsDirty { get; set; }

    #region Constructor

    /// <summary>
    /// Create new Page based on pre-defined PageID and PageType and keep buffer instance
    /// </summary>
    public BasePage(Memory<byte> buffer, uint pageID, PageType pageType)
    {
        _buffer = buffer;

        var span = buffer.Span;

        // page information
        this.PageID = pageID;
        this.PageType = pageType;

        // default values
        this.IsDirty = false;

        // writing direct into buffer in Ctor() because there is no change later (write once)
        span.Write(this.PageID, P_PAGE_ID);
        span.Write((byte)this.PageType, P_PAGE_TYPE);
    }

    /// <summary>
    /// Read header data from buffer into local variables and keep buffer instance
    /// </summary>
    public BasePage(Memory<byte> buffer)
    {
        _buffer = buffer;

        var span = buffer.Span;

        // page information
        this.PageID = span.ReadUInt32(P_PAGE_ID);
        this.PageType = (PageType)span.ReadByte(P_PAGE_TYPE);

        // defaults
        this.IsDirty = false;
    }

    #endregion

    #region Static Helpers

/*
    /// <summary>
    /// Create new page instance based on buffer (READ)
    /// </summary>
    public static T ReadPage<T>(PageBuffer buffer)
        where T : BasePage
    {
        if (typeof(T) == typeof(BasePage)) return (T)(object)new BasePage(buffer);
        if (typeof(T) == typeof(HeaderPage)) return (T)(object)new HeaderPage(buffer);
        if (typeof(T) == typeof(CollectionPage)) return (T)(object)new CollectionPage(buffer);
        if (typeof(T) == typeof(IndexPage)) return (T)(object)new IndexPage(buffer);
        if (typeof(T) == typeof(DataPage)) return (T)(object)new DataPage(buffer);

        throw new InvalidCastException();
    }

    /// <summary>
    /// Create new page instance with new PageID and passed buffer (NEW)
    /// </summary>
    public static T CreatePage<T>(PageBuffer buffer, uint pageID)
        where T : BasePage
    {
        if (typeof(T) == typeof(CollectionPage)) return (T)(object)new CollectionPage(buffer, pageID);
        if (typeof(T) == typeof(IndexPage)) return (T)(object)new IndexPage(buffer, pageID);
        if (typeof(T) == typeof(DataPage)) return (T)(object)new DataPage(buffer, pageID);

        throw new InvalidCastException();
    }
*/
    #endregion

    public override string ToString()
    {
        return $"PageID: {this.PageID.ToString().PadLeft(4, '0')} : {this.PageType}";
    }
}
