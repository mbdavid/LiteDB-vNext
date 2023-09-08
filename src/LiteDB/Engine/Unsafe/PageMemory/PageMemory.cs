namespace LiteDB.Engine;

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
unsafe internal partial struct PageMemory   // 8192
{
    [FieldOffset(00)] public uint PositionID;         // 4
    [FieldOffset(04)] public uint PageID;             // 4

    [FieldOffset(08)] public PageType PageType;       // 1
    [FieldOffset(09)] public byte ColID;              // 1
    [FieldOffset(10)] public byte ShareCounter;       // 1
    [FieldOffset(11)] public bool IsDirty;            // 1

    [FieldOffset(12)] public int UniqueID;            // 4
    [FieldOffset(16)] public int TransactionID;       // 4 

    [FieldOffset(20)] public ushort ItemsCount;       // 2
    [FieldOffset(22)] public ushort UsedBytes;        // 2
    [FieldOffset(24)] public ushort FragmentedBytes;  // 2
    [FieldOffset(26)] public ushort NextFreeLocation; // 2
    [FieldOffset(28)] public ushort HighestIndex;     // 2

    [FieldOffset(30)] public bool IsConfirmed;        // 1
    [FieldOffset(31)] public byte Crc8;               // 1

    [FieldOffset(32)] public fixed byte Buffer[PAGE_CONTENT_SIZE];   // 8160
    [FieldOffset(32)] public fixed uint Extends[PAGE_CONTENT_SIZE];  // 8160


    /// <summary>
    /// Get how many free bytes (including fragmented bytes) are in this page (content space) - Will return 0 bytes if page are full (or with max 255 items)
    /// </summary>
    public int FreeBytes => this.ItemsCount == ushort.MaxValue ? // if page is full of items (255) - returns 0 bytes empty
        0 :
        PAGE_CONTENT_SIZE - this.UsedBytes - this.FooterSize;

    /// <summary>
    /// Get how many bytes are used in footer page at this moment
    /// ((HighestIndex + 1) * 4 bytes per slot: [2 for position, 2 for length])
    /// </summary>
    public int FooterSize =>
        (this.HighestIndex == ushort.MaxValue ?
        0 :  // no items in page
        ((this.HighestIndex + 1) * sizeof(PageSegment))); // 4 bytes PER item (2 to position + 2 to length) - need consider HighestIndex used

    /// <summary>
    /// Get current extend page value based on PageType and FreeSpace
    /// </summary>
    public ExtendPageValue ExtendPageValue => PageMemory.GetExtendPageValue(this.PageType, this.FreeBytes);

    public bool IsPageInLogFile => this.PositionID != this.PageID;
    public bool IsPageInCache => this.ShareCounter != NO_CACHE;

    public PageMemory()
    {
    }

    public void Initialize(int uniqueID)
    {
        this.PositionID = uint.MaxValue;
        this.PageID = uint.MaxValue;

        this.PageType = PageType.Empty;
        this.ColID = 0;
        this.ShareCounter = byte.MaxValue;
        this.IsDirty = false;

        this.UniqueID = uniqueID;
        this.TransactionID = 0;

        this.ItemsCount = 0;
        this.UsedBytes = 0;
        this.FragmentedBytes = 0;
        this.NextFreeLocation = PAGE_HEADER_SIZE; // first location
        this.HighestIndex = ushort.MaxValue;

        this.IsConfirmed = false;
        this.Crc8 = 0;

        // clear full content area
        fixed(byte* bufferPtr = this.Buffer)
        {
            MarshalEx.FillZero(bufferPtr, PAGE_CONTENT_SIZE);
        }
    }

    public static void CopyPageContent(PageMemory* fromPtr, PageMemory* toPtr)
    {
        var uniqueID = toPtr->UniqueID;

        MarshalEx.Copy((byte*)fromPtr, (byte*)toPtr, PAGE_SIZE);

        // clean page when copy
        toPtr->UniqueID = uniqueID;
        toPtr->ShareCounter = NO_CACHE;
        toPtr->IsDirty = false;

    }

    public string DumpPage()
    {
        fixed(PageMemory* page = &this)
        {
            return PageDump.Render(page);
        }
    }

    public override string ToString()
    {
        return Dump.Object(this);
    }
}
