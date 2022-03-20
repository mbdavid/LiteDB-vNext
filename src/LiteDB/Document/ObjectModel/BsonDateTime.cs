namespace LiteDB;

/// <summary>
/// Represent a DateTime value in Bson object model
/// </summary>
public class BsonDateTime : BsonValue, IComparable<BsonDateTime>, IEquatable<BsonDateTime>
{
    public DateTime Value { get; }

    public BsonDateTime(DateTime value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.DateTime;

    public override int GetBytesCount() => 8;

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;

        if (other is BsonDateTime otherDateTime) return this.Value.CompareTo(otherDateTime.Value);

        return this.CompareType(other);
    }

    public int CompareTo(BsonDateTime other)
    {
        if (other == null) return 1;

        return this.Value.CompareTo(other.Value);
    }

    public bool Equals(BsonDateTime rhs)
    {
        if (rhs is null) return false;

        return this.Value.CompareTo(rhs.Value) == 0;
    }

    #region Explicit operators

    public static bool operator ==(BsonDateTime lhs, BsonDateTime rhs) => lhs.Equals(rhs);

    public static bool operator !=(BsonDateTime lhs, BsonDateTime rhs) => !lhs.Equals(rhs);

    #endregion

    #region Implicit Ctor

    public static implicit operator DateTime(BsonDateTime value) => value.Value;

    public static implicit operator BsonDateTime(DateTime value) => new BsonDateTime(value);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object obj) => this.Value.Equals(obj);

    public override string ToString() => this.Value.ToString();

    #endregion
}
