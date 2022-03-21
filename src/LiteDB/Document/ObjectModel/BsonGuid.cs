namespace LiteDB;

/// <summary>
/// Represent a Guid value in Bson object model
/// </summary>
public class BsonGuid : BsonValue, IComparable<BsonGuid>, IEquatable<BsonGuid>
{
    private static BsonGuid _empty = new BsonGuid(Guid.Empty);

    public static BsonGuid Empty => _empty;

    public Guid Value { get; }

    public BsonGuid(Guid value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        this.Value = value;
    }

    public override BsonType Type => BsonType.Guid;

    public override int GetBytesCount() => 16;

    #region Implement IComparable and IEquatable

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;

        if (other is BsonGuid otherGuid) return this.Value.CompareTo(otherGuid.Value);

        return this.CompareType(other);
    }

    public int CompareTo(BsonGuid other)
    {
        if (other == null) return 1;

        return this.Value.CompareTo(other.Value);
    }

    public bool Equals(BsonGuid other)
    {
        if (other is null) return false;

        return this.Value.CompareTo(other.Value) == 0;
    }

    #endregion

    #region Explicit operators

    public static bool operator ==(BsonGuid left, BsonGuid right) => left.Equals(right);

    public static bool operator !=(BsonGuid left, BsonGuid right) => !left.Equals(right);

    #endregion

    #region Implicit Ctor

    public static implicit operator Guid(BsonGuid value) => value.Value;

    public static implicit operator BsonGuid(Guid value) => new BsonGuid(value);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object other) => this.Value.Equals(other);

    public override string ToString() => this.Value.ToString();

    #endregion
}
