namespace LiteDB;

/// <summary>
/// Represent a null value constant in Bson object model (BsonNull is a valid value)
/// </summary>
public class BsonNull : BsonValue, IComparable<BsonNull>, IEquatable<BsonNull>
{
    public BsonNull()
    {
    }

    public override BsonType Type => BsonType.Null;

    public override int GetBytesCount() => 0;

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;
        if (other is BsonNull) return 0;

        return this.CompareType(other);
    }

    public int CompareTo(BsonNull other)
    {
        if (other == null) return 1;

        return 0; // singleton
    }

    public bool Equals(BsonNull other)
    {
        if (other is null) return false;

        return true;
    }

    #region Explicit operators

    public static bool operator ==(BsonNull left, BsonNull right) => left.Equals(right);

    public static bool operator !=(BsonNull left, BsonNull right) => !left.Equals(right);

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Type.GetHashCode();

    public override bool Equals(object other) => this.Equals(other as BsonNull);

    public override string ToString() => "[Null]";

    #endregion
}
