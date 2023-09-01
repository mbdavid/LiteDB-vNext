namespace LiteDB.Engine;

internal struct DataBlock2
{
    public byte DataFormat;   // 1
    public bool Extend;       // 1
    public ushort Reserved;   // 2
    public RowID NextBlockID; // 8
    // + data content
}
