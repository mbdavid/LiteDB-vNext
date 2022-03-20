namespace LiteDB;

/// <summary>
/// Represent an abstract minimal document information
/// </summary>
public abstract class BsonValue : IComparable<BsonValue>, IEquatable<BsonValue>
{
    #region Static object creator helper

    public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly BsonValue MinValue = new BsonMinValue();

    public static readonly BsonValue Null = new BsonNull();

    public static readonly BsonValue MaxValue = new BsonMaxValue();

    #endregion

    /// <summary>
    /// BsonType
    /// </summary>
    public abstract BsonType Type { get; }

    /// <summary>
    /// Get how much this Bson object will use in disk space (formated as Bson format)
    /// </summary>
    public abstract int GetBytesCount();

    #region Implement IComparable

    public int CompareTo(BsonValue other) => this.CompareTo(other, Collation.Binary);

    public abstract int CompareTo(BsonValue other, Collation collation);

    protected int CompareType(BsonValue other)
    {
        var result = this.Type.CompareTo(other.Type);
        return result < 0 ? -1 : result > 0 ? +1 : 0;
    }

    #endregion

    public bool Equals(BsonValue rhs) => this.Equals((object)rhs);

    public abstract override bool Equals(object obj);

    public abstract override int GetHashCode();

    #region Convert types

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual Int32 AsInt32 => (this as BsonInt32)?.Value ?? default(Int32);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual Int64 AsInt64 => ((BsonInt64)this).Value;

    #endregion

    #region Explicit Operators

    public static bool operator <(BsonValue lhs, BsonValue rhs)
    {
        if (lhs is null && rhs is null) return false;
        if (lhs is null) return true;
        if (rhs is null) return false;

        return lhs.CompareTo(rhs) < 0;
    }

    public static bool operator >(BsonValue lhs, BsonValue rhs) => !(lhs < rhs);

    public static bool operator <=(BsonValue lhs, BsonValue rhs)
    {
        if (lhs is null && rhs is null) return false;
        if (lhs is null) return true;
        if (rhs is null) return false;

        return lhs.CompareTo(rhs) <= 0;
    }

    public static bool operator >=(BsonValue lhs, BsonValue rhs) => !(lhs < rhs);

    #endregion

    #region Implicit Ctor

    // Int32
    public static implicit operator Int32(BsonValue value) => value.AsInt32;

    public static implicit operator BsonValue(Int32 value) => new BsonInt32(value);

    #endregion

    #region IsTypes

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsNull => this.Type == BsonType.Null;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsArray => this.Type == BsonType.Array;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsDocument => this.Type == BsonType.Document;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsInt32 => this.Type == BsonType.Int32;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsInt64 => this.Type == BsonType.Int64;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsDouble => this.Type == BsonType.Double;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsDecimal => this.Type == BsonType.Decimal;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsNumber => this.IsInt32 || this.IsInt64 || this.IsDouble || this.IsDecimal;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsBinary => this.Type == BsonType.Binary;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsBoolean => this.Type == BsonType.Boolean;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsString => this.Type == BsonType.String;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsObjectId => this.Type == BsonType.ObjectId;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsGuid => this.Type == BsonType.Guid;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsDateTime => this.Type == BsonType.DateTime;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsMinValue => this.Type == BsonType.MinValue;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsMaxValue => this.Type == BsonType.MaxValue;

    #endregion

}
