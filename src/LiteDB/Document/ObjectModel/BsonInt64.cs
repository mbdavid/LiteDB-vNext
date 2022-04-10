namespace LiteDB;

/// <summary>
/// Represent an Int64 value in Bson object model
/// </summary>
public class BsonInt64 : BsonValue
{
    public long Value { get; }

    public BsonInt64(long value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.Int64;

    public override int GetBytesCount() => sizeof(long);

    public override int GetHashCode() => this.Value.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonInt64 otherInt64) return this.Value.CompareTo(otherInt64.Value);
        if (other is BsonInt32 otherInt32) return this.Value.CompareTo(otherInt32.ToInt64());
        if (other is BsonDouble otherDouble) return this.Value.CompareTo(otherDouble.Value);
        if (other is BsonDecimal otherDecimal) return this.Value.CompareTo(otherDecimal.Value);

        return this.CompareType(other);
    }

    #endregion

    #region Implicit Ctor

    public static implicit operator long(BsonInt64 value) => value.Value;

    public static implicit operator BsonInt64(long value) => new (value);

    #endregion


    #region Convert Types

    public override bool ToBoolean() => this.Value != 0;

    public override int ToInt32() => Convert.ToInt32(this.Value);

    public override long ToInt64() => this.Value;

    public override double ToDouble() => this.Value;

    public override decimal ToDecimal() => this.Value;

    public override string ToString() => this.Value.ToString(CultureInfo.InvariantCulture.NumberFormat);

    #endregion
}
