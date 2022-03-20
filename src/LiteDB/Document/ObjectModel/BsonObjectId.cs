namespace LiteDB;

/// <summary>
/// Represent an ObjectId value (12 bytes sequencial guid-like) in Bson object model
/// </summary>
public class BsonObjectId : BsonValue, IComparable<BsonObjectId>, IEquatable<BsonObjectId>
{
    private static BsonObjectId _empty = new BsonObjectId(ObjectId.Empty);

    public static BsonObjectId Empty => _empty;

    public ObjectId Value { get; }

    public BsonObjectId(ObjectId value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        this.Value = value;
    }

    public override BsonType Type => BsonType.ObjectId;

    public override int GetBytesCount() => 12;

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;

        if (other is BsonObjectId otherObjectId) return this.Value.CompareTo(otherObjectId.Value);

        return this.CompareType(other);
    }

    public int CompareTo(BsonObjectId other)
    {
        if (other == null) return 1;

        return this.Value.CompareTo(other.Value);
    }

    public bool Equals(BsonObjectId other)
    {
        if (other is null) return false;

        return this.Value.CompareTo(other.Value) == 0;
    }

    #region Explicit operators

    public static bool operator ==(BsonObjectId left, BsonObjectId right) => left.Equals(right);

    public static bool operator !=(BsonObjectId left, BsonObjectId right) => !left.Equals(right);

    #endregion

    #region Implicit Ctor

    public static implicit operator ObjectId(BsonObjectId value) => value.Value;

    public static implicit operator BsonObjectId(ObjectId value) => new BsonObjectId(value);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object other) => this.Equals(other as BsonObjectId);

    public override string ToString() => this.Value.ToString();

    #endregion
}
