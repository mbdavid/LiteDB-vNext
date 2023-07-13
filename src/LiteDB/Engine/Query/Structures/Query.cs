namespace LiteDB.Engine;

/// <summary>
/// </summary>
public class Query : IQuery
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

    public BsonExpression Select { get; init; } = BsonExpression.Empty;
    public BsonExpression[] Includes { get; init; } = Array.Empty<BsonExpression>();
    public BsonExpression Where { get; init; } = BsonExpression.Empty;
    public BsonExpression OrderBy { get; init; } = BsonExpression.Empty;
    public int Order { get; init; } = Query.Ascending;
    public int Offset { get; init; } = 0;
    public int Limit { get; init; } = int.MaxValue;
}