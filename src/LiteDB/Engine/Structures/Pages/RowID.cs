namespace LiteDB.Engine;

internal unsafe struct RowID
{
    public uint PageID; // 4
    public ushort Index; // 2
    public ushort Reserved; // 2
}
