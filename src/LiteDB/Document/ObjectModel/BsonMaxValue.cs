namespace LiteDB;

/// <summary>
/// Represent a min value constant in Bson object model
/// </summary>
public class BsonMinValue : BsonValue, IComparable<BsonMinValue>, IEquatable<BsonMinValue>
{
    public BsonMinValue()
    {
    }

    public override BsonType Type => BsonType.MinValue;

    public override int GetBytesCount() => 0;

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;
        if (other is BsonMinValue) return 0;

        return -1; // all types are heigher than MinValue
    }

    public int CompareTo(BsonMinValue other)
    {
        if (other == null) return 1;

        return 0; // singleton
    }

    public bool Equals(BsonMinValue rhs)
    {
        if (rhs is null) return false;

        return true;
    }

    #region Explicit operators

    public static bool operator ==(BsonMinValue lhs, BsonMinValue rhs) => lhs.Equals(rhs);

    public static bool operator !=(BsonMinValue lhs, BsonMinValue rhs) => !lhs.Equals(rhs);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Type.GetHashCode();

    public override bool Equals(object obj) => this.Equals(obj as BsonMinValue);

    public override string ToString() => "[MinValue]";

    #endregion
}
