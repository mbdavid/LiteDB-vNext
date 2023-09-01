namespace LiteDB.Engine;

internal struct IndexNodeLevel
{
    public RowID PrevID; // 8
    public RowID NextID; // 8
}
