namespace LiteDB;

/// <summary>
/// Represent a String value in Bson object model
/// </summary>
public class BsonString : BsonValue, IComparable<BsonString>, IEquatable<BsonString>
{
    private static BsonString _empty = new BsonString("");

    public static BsonString Emtpy => _empty;

    public string Value { get; }

    public BsonString(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        this.Value = value;
    }

    public override BsonType Type => BsonType.String;

    public override int GetBytesCount() => Encoding.UTF8.GetByteCount(this.Value);

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

    public bool Equals(BsonString rhs)
    {
        if (rhs is null) return false;

        return this.Value == rhs.Value;
    }

    #region Explicit operators

    public static bool operator ==(BsonString lhs, BsonString rhs) => lhs.Equals(rhs);

    public static bool operator !=(BsonString lhs, BsonString rhs) => !lhs.Equals(rhs);

    #endregion

    #region Implicit Ctor

    public static implicit operator String(BsonString value) => value.Value;

    public static implicit operator BsonString(String value) => new BsonString(value);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object obj) => this.Value.Equals(obj);

    public override string ToString() => this.Value;

    #endregion
}
