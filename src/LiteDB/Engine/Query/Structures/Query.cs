namespace LiteDB.Engine;

/// <summary>
/// </summary>
public partial class Query
{
    public BsonExpression Select { get; init; } = BsonExpression.Empty;
    public BsonExpression[] Includes { get; init; } = Array.Empty<BsonExpression>();
    public BsonExpression Where { get; init; } = BsonExpression.Empty;
    public BsonExpression OrderBy { get; init; } = BsonExpression.Empty;
    public int Order { get; init; } = Query.Ascending;
    public int Offset { get; init; } = 0;
    public int Limit { get; init; } = int.MaxValue;
}