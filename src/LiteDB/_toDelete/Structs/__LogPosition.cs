namespace LiteDB.Engine;

internal struct __LogPosition : IEqualityComparer<__LogPosition>
{
    public int PositionID;
    public int PageID;
    public int PhysicalID;
    public bool IsConfirmed;

    public bool Equals(__LogPosition x, __LogPosition y)
    {
        return x.PositionID == y.PositionID;
    }

    public int GetHashCode(__LogPosition obj)
    {
        return obj.PositionID;
    }

    public override string ToString()
    {
        return Dump.Object(new { PhysicalID = Dump.PageID(PhysicalID), PositionID = Dump.PageID(PositionID), PageID = Dump.PageID(PageID), IsConfirmed });
    }
}
