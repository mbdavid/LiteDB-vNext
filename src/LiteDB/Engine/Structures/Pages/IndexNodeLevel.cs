namespace LiteDB.Engine;

internal struct IndexNodeLevel // 16
{
    public RowID PrevID; // 8
    public RowID NextID; // 8
}
