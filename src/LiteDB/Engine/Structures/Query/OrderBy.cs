namespace LiteDB.Engine;

/// <summary>
/// Represent an OrderBy definition
/// </summary>
public struct OrderBy
{
    public static OrderBy Empty = new(BsonExpression.Empty, 0);

    public BsonExpression Expression { get; }

    public int Order { get; set; }

    public bool IsEmpty => this.Expression.IsEmpty && this.Order == 0;

    public OrderBy(BsonExpression expression, int order)
    {
        this.Expression = expression;
        this.Order = order;
    }

    public override string ToString() => Dump.Object(this);
}
