namespace LiteDB.Engine;

internal class Query_2
{
    #region Constants

    /// <summary>
    /// Indicate when a query must execute in ascending order
    /// </summary>
    public const int Ascending = 1;

    /// <summary>
    /// Indicate when a query must execute in descending order
    /// </summary>
    public const int Descending = -1;

    #endregion

    public required IDocumentStore Source { get; init; }
    public List<(string key, BsonExpression expr)> Select { get; } = new();
    public bool Distinct { get; init; }
    public (IDocumentStore store, BsonAutoId autoId)? Into { get; init; } = default;
    public BsonExpression[] Includes { get; init; } = Array.Empty<BsonExpression>();
    public BsonExpression Where { get; init; } = BsonExpression.Empty;
    public BsonExpression GroupBy { get; set; } = BsonExpression.Empty;
    public BsonExpression Having { get; init; } = BsonExpression.Empty;
    public OrderBy OrderBy { get; init; } = OrderBy.Empty;
    public int Offset { get; init; } = 0;
    public int Limit { get; init; } = int.MaxValue;

    #region Helper computed properties

    /// <summary>
    /// Get if this query will use Aggregate functions
    /// </summary>
    public bool IsAggregate => this.GroupBy.IsEmpty != false || this.Select.Any(f => f.expr.IsAggregateCall);

    #endregion

}
