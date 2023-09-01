namespace LiteDB.Engine;

internal unsafe struct PageMemory
{
    public uint PositionID;         // 4
    public uint PageID;             // 4

    public PageType PageType;       // 1
    public byte ColID;              // 1
    public byte ShareCounter;       // 1
    public bool IsDirty;            // 1

    public uint TempPosID;          // 4
    public uint TransactionID;      // 4 

    public ushort ItemsCount;       // 2
    public ushort UsedBytes;        // 2
    public ushort FragmentedBytes;  // 2
    public ushort NextFreeLocation; // 2
    public ushort HighestIndex;     // 2

    public bool IsConfirmed;        // 1
    public byte Crc8;               // 1

    public fixed byte Buffer[PAGE_CONTENT_SIZE]; // 8160

    public PageMemory()
    {
    }
}
