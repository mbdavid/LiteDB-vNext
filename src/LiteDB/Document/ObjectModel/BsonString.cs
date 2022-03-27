namespace LiteDB;

/// <summary>
/// Represent a String value in Bson object model
/// </summary>
public class BsonString : BsonValue, IComparable<BsonString>, IEquatable<BsonString>
{
    public static BsonString Emtpy = new("");

    public string Value { get; }

    public BsonString(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        this.Value = value;
    }

    public override BsonType Type => BsonType.String;

    public override int GetBytesCount() => Encoding.UTF8.GetByteCount(this.Value);

    #region Implement IComparable and IEquatable

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;

        if (other is BsonString otherString) return collation.Compare(this.Value, otherString.Value);

        return this.CompareType(other);
    }

    public int CompareTo(BsonString other)
    {
        if (other == null) return 1;

        return this.Value.CompareTo(other.Value);
    }

    public bool Equals(BsonString other)
    {
        if (other is null) return false;

        return this.Value == other.Value;
    }

    #endregion

    #region Explicit operators

    public static bool operator ==(BsonString left, BsonString right) => left.Equals(right);

    public static bool operator !=(BsonString left, BsonString right) => !left.Equals(right);

    #endregion

    #region Implicit Ctor

    public static implicit operator String(BsonString value) => value.Value;

    public static implicit operator BsonString(String value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        if (value.Length == 0) return Emtpy;

        return new BsonString(value);
    }

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object other) => this.Equals(other as BsonString);

    public override string ToString() => this.Value;

    #endregion
}
