namespace LiteDB.Engine;

internal struct IndexNodeLevel // 16
{
    public RowID PrevID; // 8
    public RowID NextID; // 8

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RowID GetNextPrev(int order)
    {
        return order > 0 ? this.NextID : this.PrevID;
    }
}
