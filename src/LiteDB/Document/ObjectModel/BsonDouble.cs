namespace LiteDB;

/// <summary>
/// Represent a double value in Bson object model
/// </summary>
public class BsonDouble : BsonValue, IComparable<BsonDouble>, IEquatable<BsonDouble>
{
    public Double Value { get; }

    public BsonDouble(Double value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.Double;

    public override int GetBytesCount() => sizeof(Double);

    #region Implement IComparable and IEquatable

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;

        if (other is BsonDouble otherDouble) return this.Value.CompareTo(otherDouble.Value);
        if (other is BsonInt32 otherInt32) return this.Value.CompareTo(otherInt32.Value);
        if (other is BsonInt64 otherInt64) return this.Value.CompareTo(otherInt64.Value);
        if (other is BsonDecimal otherDecimal) return this.Value.CompareTo(otherDecimal.Value);

        return this.CompareType(other);
    }

    public int CompareTo(BsonDouble other)
    {
        if (other == null) return 1;

        return this.Value.CompareTo(other.Value);
    }

    public bool Equals(BsonDouble other)
    {
        if (other is null) return false;

        return this.Value == other.Value;
    }

    #endregion

    #region Explicit operators

    public static bool operator ==(BsonDouble left, BsonDouble right) => left.Equals(right);

    public static bool operator !=(BsonDouble left, BsonDouble right) => !left.Equals(right);

    #endregion

    #region Implicit Ctor

    public static implicit operator Double(BsonDouble value) => value.Value;

    public static implicit operator BsonDouble(Double value) => new BsonDouble(value);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object other) => this.Value.Equals(other);

    public override string ToString() => this.Value.ToString();

    #endregion
}
