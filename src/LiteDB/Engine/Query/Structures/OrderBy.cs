namespace LiteDB.Engine;

/// <summary>
/// Represent an OrderBy definition
/// </summary>
internal struct OrderBy
{
    public BsonExpression Expression { get; }

    public int Order { get; set; }

    public OrderBy(BsonExpression expression, int order)
    {
        this.Expression = expression;
        this.Order = order;
    }
}
