namespace LiteDB;

/// <summary>
/// Represent an integer value in Bson object model
/// </summary>
internal class BsonDecimal : BsonValue
{
    public decimal Value { get; }

    public BsonDecimal(decimal value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.Decimal;

    public override int GetBytesCount() => sizeof(decimal);

    public override int GetHashCode() => this.Value.GetHashCode();

    #region Implement IComparable

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonDecimal otherDecimal) return this.Value.CompareTo(otherDecimal.Value);
        if (other is BsonInt32 otherInt32) return this.Value.CompareTo(otherInt32.ToDecimal());
        if (other is BsonInt64 otherInt64) return this.Value.CompareTo(otherInt64.ToDecimal());
        if (other is BsonDouble otherDouble) return this.Value.CompareTo(otherDouble.ToDecimal());

        return this.CompareType(other);
    }

    #endregion

    #region Convert Types

    public override bool ToBoolean() => this.Value != 0;

    public override int ToInt32() => Convert.ToInt32(this.Value);

    public override long ToInt64() => Convert.ToInt64(this.Value);

    public override double ToDouble() => Convert.ToDouble(this.Value);

    public override decimal ToDecimal() => this.Value;

    public override string ToString() => this.Value.ToString(CultureInfo.InvariantCulture.NumberFormat);

    #endregion
}
