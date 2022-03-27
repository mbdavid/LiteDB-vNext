namespace LiteDB;

/// <summary>
/// Represent an integer value in Bson object model
/// </summary>
public class BsonDecimal : BsonValue, IComparable<BsonDecimal>, IEquatable<BsonDecimal>
{
    public Decimal Value { get; }

    public BsonDecimal(Decimal value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.Decimal;

    public override int GetBytesCount() => sizeof(Decimal);

    #region Implement IComparable and IEquatable

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;

        if (other is BsonDecimal otherDecimal) return this.Value.CompareTo(otherDecimal.Value);
        if (other is BsonInt32 otherInt32) return this.Value.CompareTo(otherInt32.Value);
        if (other is BsonInt64 otherInt64) return this.Value.CompareTo(otherInt64.Value);
        if (other is BsonUInt64 otherUInt64) return this.Value.CompareTo(otherUInt64.Value);
        if (other is BsonDouble otherDouble) return this.Value.CompareTo(otherDouble.Value);

        return this.CompareType(other);
    }

    public int CompareTo(BsonDecimal other)
    {
        if (other == null) return 1;

        return this.Value.CompareTo(other.Value);
    }

    public bool Equals(BsonDecimal other)
    {
        if (other is null) return false;

        return this.Value == other.Value;
    }

    #endregion

    #region Explicit operators

    public static bool operator ==(BsonDecimal left, BsonDecimal right) => left.Equals(right);

    public static bool operator !=(BsonDecimal left, BsonDecimal right) => !left.Equals(right);

    #endregion

    #region Implicit Ctor

    public static implicit operator Decimal(BsonDecimal value) => value.Value;

    public static implicit operator BsonDecimal(decimal value) => new (value);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object other) => this.Equals(other as BsonDecimal);

    public override string ToString() => this.Value.ToString();

    #endregion
}
