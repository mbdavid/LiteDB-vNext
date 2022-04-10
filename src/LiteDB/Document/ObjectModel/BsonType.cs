namespace LiteDB;

/// <summary>
/// All supported BsonTypes in sort order
/// </summary>
public enum BsonType : byte
{
    MinValue = 0,

    Null = 1,

    Int32 = 2,
    Int64 = 3,
    Double = 5,
    Decimal = 6,

    String = 7,

    Document = 8,
    Array = 9,

    Binary = 10,
    ObjectId = 11,
    Guid = 12,

    Boolean = 13,
    DateTime = 14,

    MaxValue = 15
}