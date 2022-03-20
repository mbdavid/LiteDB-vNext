namespace LiteDB;

/// <summary>
/// Represent a max value constant in Bson object model
/// </summary>
public class BsonMaxValue : BsonValue, IComparable<BsonMaxValue>, IEquatable<BsonMaxValue>
{
    public BsonMaxValue()
    {
    }

    public override BsonType Type => BsonType.MaxValue;

    public override int GetBytesCount() => 0;

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;
        if (other is BsonMaxValue) return 0;

        return 1; // all types are lower than MaxValue
    }

    public int CompareTo(BsonMaxValue other)
    {
        if (other == null) return 1;

        return 0; // singleton
    }

    public bool Equals(BsonMaxValue rhs)
    {
        if (rhs is null) return false;

        return true;
    }

    #region Explicit operators

    public static bool operator ==(BsonMaxValue lhs, BsonMaxValue rhs) => lhs.Equals(rhs);

    public static bool operator !=(BsonMaxValue lhs, BsonMaxValue rhs) => !lhs.Equals(rhs);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Type.GetHashCode();

    public override bool Equals(object obj) => this.Equals(obj as BsonMaxValue);

    public override string ToString() => "[MaxValue]";

    #endregion
}
