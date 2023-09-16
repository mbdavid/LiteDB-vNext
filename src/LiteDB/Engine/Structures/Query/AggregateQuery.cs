namespace LiteDB.Engine;

/// <summary>
/// </summary>
public class AggregateQuery : Query
{
    public BsonExpression Key { get; init; } = BsonExpression.Empty;
    public IAggregateFunc[] Functions { get; init; } = Array.Empty<IAggregateFunc>();
    public BsonExpression Having { get; init; } = BsonExpression.Empty;
}