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

    public static void Initialize(PageMemory* pageMemoryPtr, int uniqueID)
    {
        pageMemoryPtr->PositionID = int.MaxValue;
        pageMemoryPtr->PageID = int.MaxValue;

        pageMemoryPtr->PageType = PageType.Empty;
        pageMemoryPtr->ColID = 0;
        pageMemoryPtr->ShareCounter = byte.MaxValue;
        pageMemoryPtr->IsDirty = false;

        pageMemoryPtr->UniqueID = uniqueID;
        pageMemoryPtr->TransactionID = 0;

        pageMemoryPtr->ItemsCount = 0;
        pageMemoryPtr->UsedBytes = 0;
        pageMemoryPtr->FragmentedBytes = 0;
        pageMemoryPtr->NextFreeLocation = PAGE_HEADER_SIZE; // first location
        pageMemoryPtr->HighestIndex = ushort.MaxValue;

        pageMemoryPtr->IsConfirmed = false;
        pageMemoryPtr->Crc8 = 0;

        // get content pointer
        var contentPtr = (byte*)(((nint)pageMemoryPtr) + PAGE_HEADER_SIZE);

        // clear full content area
        var content = new Span<byte>(contentPtr, PAGE_CONTENT_SIZE);
        content.Fill(0);
    }
}
