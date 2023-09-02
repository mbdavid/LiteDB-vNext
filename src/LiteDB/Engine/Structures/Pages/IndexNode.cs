namespace LiteDB.Engine;

internal struct IndexNode     // 20
{
    public byte Slot;         // 1
    public byte Levels;       // 1
    public byte KeyDataType;  // 1
    public byte KeyLength;    // 1

    public RowID DataBlockID; // 8
    public RowID NextNodeID;  // 8
    // + (Levels * IndexNodeLevel)
    // + 1 data type (do not support array/document)
    // + 1 length (for string/byte)
    // + content
}
