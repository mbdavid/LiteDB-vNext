namespace LiteDB;

/// <summary>
/// Represent a Boolean value in Bson object model
/// </summary>
public class BsonBoolean : BsonValue, IComparable<BsonBoolean>, IEquatable<BsonBoolean>
{
    public static BsonBoolean True = new BsonBoolean(true);
    public static BsonBoolean False = new BsonBoolean(false);

    public bool Value { get; }

    public BsonBoolean(bool value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.Boolean;

    public override int GetBytesCount() => sizeof(bool);

    #region Implement IComparable and IEquatable

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;

        if (other is BsonBoolean otherBoolean) return this.Value.CompareTo(otherBoolean.Value);

        return this.CompareType(other);
    }

    public int CompareTo(BsonBoolean other)
    {
        if (other == null) return 1;

        return this.Value.CompareTo(other.Value);
    }

    public bool Equals(BsonBoolean other)
    {
        if (other is null) return false;

        return this.Value == other.Value;
    }

    #endregion

    #region Explicit operators

    public static bool operator ==(BsonBoolean left, BsonBoolean right) => left.Equals(right);

    public static bool operator !=(BsonBoolean left, BsonBoolean right) => !left.Equals(right);

    #endregion

    #region Implicit Ctor

    public static implicit operator Boolean(BsonBoolean value) => value.Value;

    public static implicit operator BsonBoolean(Boolean value) => value ? True : False;

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object other) => this.Value.Equals(other);

    public override string ToString() => this.Value.ToString();

    #endregion
}
