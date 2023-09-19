namespace LiteDB.Engine;

public class Query_2
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

    public required ISourceStore Source { get; init; }
    public List<(string key, ISelectField field)> Select { get; } = new();
    public bool Distinct { get; init; }
    public ITargetStore? Into { get; init; } = default;
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
    public bool IsAggregate => this.GroupBy.IsEmpty == false || this.Select.Any(f => f.field is AggregateSelectField);

    #endregion

}
