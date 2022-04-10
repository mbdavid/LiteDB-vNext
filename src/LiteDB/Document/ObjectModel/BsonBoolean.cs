namespace LiteDB;

/// <summary>
/// Represent a Boolean value in Bson object model
/// </summary>
public class BsonBoolean : BsonValue
{
    public static readonly BsonBoolean True = new (true);
    public static readonly BsonBoolean False = new (false);

    public bool Value { get; }

    public BsonBoolean(bool value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.Boolean;

    public override int GetBytesCount() => sizeof(bool);

    public override int GetHashCode() => this.Value.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;

        if (other is BsonBoolean otherBoolean) return this.Value.CompareTo(otherBoolean.Value);

        return this.CompareType(other);
    }

    #endregion

    #region Implicit Ctor

    public static implicit operator bool(BsonBoolean value) => value.Value;

    public static implicit operator BsonBoolean(bool value) => value ? True : False;

    #endregion

    #region GetHashCode, Equals, ToString override

    public override bool ToBoolean() => this.Value;

    public override int ToInt32() => this.Value ? 1 : 0;

    public override long ToInt64() => this.Value ? 1 : 0;

    public override double ToDouble() => this.Value ? 1 : 0;

    public override decimal ToDecimal() => this.Value ? 1 : 0;

    public override string ToString() => this.Value.ToString(CultureInfo.InvariantCulture.NumberFormat);

    #endregion
}
