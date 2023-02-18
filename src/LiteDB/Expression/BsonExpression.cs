namespace LiteDB;

public abstract partial class BsonExpression
{
    public abstract BsonExpressionType Type { get; }

    internal virtual IEnumerable<BsonExpression> Children => new BsonExpression[0];

    /// <summary>
    /// Only internal ctor (from BsonParserExpression)
    /// </summary>
    internal BsonExpression()
    {
    }

    /// <summary>
    /// Implicit string converter
    /// </summary>
    public static implicit operator string(BsonExpression expr) => expr.ToString();

    /// <summary>
    /// Implicit string converter
    /// </summary>
    public static implicit operator BsonExpression(string expr) => BsonExpression.Create(expr);

    internal abstract BsonValue Execute(BsonExpressionContext context);

    public BsonValue Execute(BsonValue? root = null, BsonDocument? parameters = null, Collation? collation = null)
    {
        var context = new BsonExpressionContext(root, parameters, collation);

        return this.Execute(context);
    }
}
