namespace LiteDB;

/// <summary>
/// Represent a DateTime value in Bson object model
/// </summary>
public class BsonDateTime : BsonValue, IComparable<BsonDateTime>, IEquatable<BsonDateTime>
{
    public static readonly DateTime UnixEpoch = new (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public DateTime Value { get; }

    public BsonDateTime(DateTime value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.DateTime;

    public override int GetBytesCount() => 8;

    #region Implement IComparable and IEquatable

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

    public bool Equals(BsonDateTime other)
    {
        if (other is null) return false;

        return this.Value.CompareTo(other.Value) == 0;
    }

    #endregion

    #region Explicit operators

    public static bool operator ==(BsonDateTime left, BsonDateTime right) => left.Equals(right);

    public static bool operator !=(BsonDateTime left, BsonDateTime right) => !left.Equals(right);

    #endregion

    #region Implicit Ctor

    public static implicit operator DateTime(BsonDateTime value) => value.Value;

    public static implicit operator BsonDateTime(DateTime value) => new (value);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object other) => this.Equals(other as BsonDateTime);

    public override string ToString() => this.Value.ToString();

    #endregion
}
