namespace LiteDB.Engine;

internal struct IndexNode     // 20
{
    public byte Slot;         // 1
    public byte Levels;       // 1
    public ushort Reserved;   // 2

    public RowID DataBlockID; // 8
    public RowID NextNodeID;  // 8
                              // + (Levels * IndexNodeLevel)
                              // + IndexKey


    #region Static Helpers

    /// <summary>
    /// Calculate how many bytes this node will need on page block
    /// </summary>
    public unsafe static int GetNodeLength(int levels, IndexKey key)
    {
        return 
            sizeof(IndexNode) +
            (levels * sizeof(IndexNodeLevel)) + // prev/next
            2 + // IndexKey (DataType+Length)
            key.Length; // key
    }

    #endregion
}
