namespace LiteDB.Engine;

/// <summary>
/// </summary>
public partial class AggregateQuery
{
    public BsonExpression Where { get; init; } = BsonExpression.Empty;
    public BsonExpression Key { get; init; } = BsonExpression.Empty;
    public IAggregateFunc[] Functions { get; init; } = Array.Empty<IAggregateFunc>();
    public BsonExpression Having { get; init; } = BsonExpression.Empty; 
    public int Offset { get; init; } = 0;
    public int Limit { get; init; } = int.MaxValue;
}