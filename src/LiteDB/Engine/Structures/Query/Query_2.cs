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
    public SelectFields Select { get; init; } = SelectFields.Default;
    public bool Distinct { get; init; } = false;
    public Into Into { get; init; } = Into.Empty;
    public BsonExpression[] Includes { get; init; } = Array.Empty<BsonExpression>();
    public BsonExpression Where { get; init; } = BsonExpression.Empty;
    public BsonExpression GroupBy { get; set; } = BsonExpression.Empty;
    public BsonExpression Having { get; init; } = BsonExpression.Empty;
    public OrderBy OrderBy { get; init; } = OrderBy.Empty;
    public int Offset { get; init; } = 0;
    public int Limit { get; init; } = int.MaxValue;

}
