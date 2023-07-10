namespace LiteDB; // the "Engine" sufix name was not used to maintain compatibility with previous versions

/// <summary>
/// </summary>
public partial class Query
{
    public BsonExpression Select { get; set; } = BsonExpression.Empty;
    public BsonExpression Where { get; set; } = BsonExpression.Empty;
    public BsonExpression OrderBy { get; set; } = BsonExpression.Empty;
    public int Order { get; set; } = Query.Ascending;
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = int.MaxValue;
}