namespace LiteDB.Engine;

internal struct LogPosition : IEqualityComparer<LogPosition>
{
    public int PositionID;
    public int PageID;
    public int PhysicalID;
    public bool IsConfirmed;

    public bool Equals(LogPosition x, LogPosition y)
    {
        return x.PositionID == y.PositionID;
    }

    public int GetHashCode(LogPosition obj)
    {
        return obj.PositionID;
    }

    public override string ToString()
    {
        return Dump.Object(new { PhysicalID = Dump.PageID(PhysicalID), PositionID = Dump.PageID(PositionID), PageID = Dump.PageID(PageID), IsConfirmed });
    }
}
