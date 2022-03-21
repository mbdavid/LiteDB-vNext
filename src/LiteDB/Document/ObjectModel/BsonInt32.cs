namespace LiteDB;

/// <summary>
/// Represent an integer value in Bson object model
/// </summary>
public class BsonInt32 : BsonValue, IComparable<BsonInt32>, IEquatable<BsonInt32>
{
    public Int32 Value { get; }

    public BsonInt32(Int32 value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.Int32;

    public override int GetBytesCount() => sizeof(Int32);

    #region Implement IComparable and IEquatable

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;

        if (other is BsonInt32 otherInt32) return this.Value.CompareTo(otherInt32.Value);
        if (other is BsonInt64 otherInt64) return this.Value.CompareTo(otherInt64.Value);
        if (other is BsonDouble otherDouble) return this.Value.CompareTo(otherDouble.Value);
        if (other is BsonDecimal otherDecimal) return this.Value.CompareTo(otherDecimal.Value);

        return this.CompareType(other);
    }

    public int CompareTo(BsonInt32 other)
    {
        if (other == null) return 1;

        return this.Value.CompareTo(other.Value);
    }

    public bool Equals(BsonInt32 other)
    {
        if (other is null) return false;

        return this.Value == other.Value;
    }

    #endregion

    #region Explicit operators

    public static bool operator ==(BsonInt32 left, BsonInt32 right) => left.Equals(right);

    public static bool operator !=(BsonInt32 left, BsonInt32 right) => !left.Equals(right);

    #endregion

    #region Implicit Ctor

    public static implicit operator Int32(BsonInt32 value) => value.Value;

    public static implicit operator BsonInt32(Int32 value) => new BsonInt32(value);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object other) => this.Value.Equals(other);

    public override string ToString() => this.Value.ToString();

    #endregion
}
