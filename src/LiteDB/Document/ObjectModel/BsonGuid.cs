namespace LiteDB;

/// <summary>
/// Represent a Guid value in Bson object model
/// </summary>
public class BsonGuid : BsonValue
{
    public static BsonGuid Empty = new BsonGuid(Guid.Empty);

    public Guid Value { get; }

    public BsonGuid(Guid value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        this.Value = value;
    }

    public override BsonType Type => BsonType.Guid;

    public override int GetBytesCount() => 16;

    public override int GetHashCode() => this.Value.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonGuid otherGuid) return this.Value.CompareTo(otherGuid.Value);

        return this.CompareType(other);
    }

    #endregion

    #region Implicit Ctor

    public static implicit operator Guid(BsonGuid value) => value.Value;

    public static implicit operator BsonGuid(Guid value) => new BsonGuid(value);

    #endregion

    #region Convert Types

    public override string ToString() => this.Value.ToString();

    #endregion
}
