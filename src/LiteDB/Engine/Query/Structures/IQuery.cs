namespace LiteDB.Engine;

/// <summary>
/// </summary>
public interface IQuery
{
    BsonExpression Select { get; }
    BsonExpression Where { get; }
    int Offset { get; }
    int Limit { get; }
}