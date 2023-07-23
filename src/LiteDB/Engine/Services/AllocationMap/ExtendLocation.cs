using System.Net.Http.Headers;

namespace LiteDB.Engine;

internal struct ExtendLocation : IComparable<ExtendLocation>, IEquatable<ExtendLocation>
{
    public static ExtendLocation Empty = new(-1, -1);
    public static ExtendLocation First = new(0, 0);

    public readonly int AllocationMapID;
    public readonly int ExtendIndex;

    public int ExtendID => this.IsEmpty ? -1 : (this.AllocationMapID * AM_EXTEND_COUNT) + this.ExtendIndex;

    public ExtendLocation(int allocationMapID, int extendIndex)
    {
        this.AllocationMapID = allocationMapID;
        this.ExtendIndex = extendIndex;
    }

    public bool IsEmpty => 
        this.AllocationMapID == Empty.AllocationMapID && 
        this.ExtendIndex == Empty.ExtendIndex;

    public ExtendLocation Next()
    {
        var allocationMapID = this.AllocationMapID;
        var extendIndex = this.ExtendIndex + 1;

        if (extendIndex >= AM_EXTEND_COUNT)
        {
            extendIndex = 0;
            allocationMapID++;
        }

        return new ExtendLocation(allocationMapID, extendIndex);
    }

    public override string ToString()
    {
        return $"AMP: {this.AllocationMapID}, ExtIndex: {this.ExtendIndex}";
    }

    #region ICompare, IEquality, explicit operators

    public static bool operator ==(ExtendLocation left, ExtendLocation right) => left.Equals(right);

    public static bool operator !=(ExtendLocation left, ExtendLocation right) => !left.Equals(right);

    public static bool operator <(ExtendLocation left, ExtendLocation right) => left.CompareTo(right) < 0;

    public static bool operator >(ExtendLocation left, ExtendLocation right) => left.CompareTo(right) > 0;

    public static bool operator <=(ExtendLocation left, ExtendLocation right) => left.CompareTo(right) <= 0;

    public static bool operator >=(ExtendLocation left, ExtendLocation right) => left.CompareTo(right) >= 0;

    public int CompareTo(ExtendLocation other)
    {
        if (this.IsEmpty && other.IsEmpty) return 0;
        if (this.IsEmpty) return 1;
        if (other.IsEmpty) return -1;

        var diff = this.ExtendID - other.ExtendID;

        return diff switch
        {
            < 0 => -1,
            0 => 0,
            _ => -1
        };
    }

    public bool Equals(ExtendLocation other) =>
        this.AllocationMapID == other.AllocationMapID && 
        this.ExtendIndex == other.ExtendIndex;

    public override bool Equals(object other) => other is ExtendLocation loc && this.Equals(loc);

    public override int GetHashCode() => HashCode.Combine(this.AllocationMapID, this.ExtendIndex);

    #endregion
}
