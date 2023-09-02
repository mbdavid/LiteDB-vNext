namespace LiteDB.Engine;

internal unsafe struct PageMemory   // 8192
{
    public uint PositionID;         // 4
    public uint PageID;             // 4

    public PageType PageType;       // 1
    public byte ColID;              // 1
    public byte ShareCounter;       // 1
    public bool IsDirty;            // 1

    public int UniqueID;            // 4
    public int TransactionID;       // 4 

    public ushort ItemsCount;       // 2
    public ushort UsedBytes;        // 2
    public ushort FragmentedBytes;  // 2
    public ushort NextFreeLocation; // 2
    public ushort HighestIndex;     // 2

    public bool IsConfirmed;        // 1
    public byte Crc8;               // 1

    public fixed byte Buffer[PAGE_CONTENT_SIZE]; // 8160
    public fixed uint Extends[PAGE_CONTENT_SIZE]; // 8160


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
    public ExtendPageValue ExtendPageValue => AllocationMapPage.GetExtendPageValue(this.PageType, this.FreeBytes);

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
}
