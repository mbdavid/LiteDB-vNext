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

    #region Implement IComparable and IEquatable

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

    public bool Equals(BsonMaxValue other)
    {
        if (other is null) return false;

        return true;
    }

    #endregion

    #region Explicit operators

    public static bool operator ==(BsonMaxValue left, BsonMaxValue right) => left.Equals(right);

    public static bool operator !=(BsonMaxValue left, BsonMaxValue right) => !left.Equals(right);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Type.GetHashCode();

    public override bool Equals(object other) => this.Equals(other as BsonMaxValue);

    public override string ToString() => "[MaxValue]";

    #endregion
}
