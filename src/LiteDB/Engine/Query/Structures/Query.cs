namespace LiteDB; // the "Engine" sufix name was not used to maintain compatibility with previous versions

/// <summary>
/// </summary>
public partial class Query
{
    public BsonExpression Select { get; init; } = BsonExpression.Empty;
    public BsonExpression Where { get; init; } = BsonExpression.Empty;
    public BsonExpression OrderBy { get; init; } = BsonExpression.Empty;
    public int Order { get; init; } = Query.Ascending;
    public int Offset { get; init; } = 0;
    public int Limit { get; init; } = int.MaxValue;
}