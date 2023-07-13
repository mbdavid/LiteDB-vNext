namespace LiteDB.Engine;

/// <summary>
/// </summary>
public interface IQuery
{
    BsonExpression Select { get; init; }
    BsonExpression Where { get; init; }
    int Offset { get; init; }
    int Limit { get; init; }
}