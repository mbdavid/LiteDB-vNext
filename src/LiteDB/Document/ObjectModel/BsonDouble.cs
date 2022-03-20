namespace LiteDB;

/// <summary>
/// Represent an integer value in Bson object model
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

    public bool Equals(BsonDouble rhs)
    {
        if (rhs is null) return false;

        return this.Value == rhs.Value;
    }

    #region Explicit operators

    public static bool operator ==(BsonDouble lhs, BsonDouble rhs) => lhs.Equals(rhs);

    public static bool operator !=(BsonDouble lhs, BsonDouble rhs) => !lhs.Equals(rhs);

    #endregion

    #region Implicit Ctor

    public static implicit operator Double(BsonDouble value) => value.Value;

    public static implicit operator BsonDouble(Double value) => new BsonDouble(value);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object obj) => this.Value.Equals(obj);

    public override string ToString() => this.Value.ToString();

    #endregion
}
