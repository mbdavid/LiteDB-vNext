using LiteDB.Engine;

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

    #region Internal Uses

    /// <summary>
    /// Execute expression and return a ienumerable of distinct values (convert array into multiple values)
    /// </summary>
    internal IEnumerable<BsonValue> GetIndexKeys(BsonDocument root, Collation collation)
    {
        var keys = this.Execute(root, null, collation);

        if (keys.IsArray)
        {
            foreach (var key in keys.AsArray)
            {
                yield return key;
            }
        }
        else
        {
            yield return keys;
        }
    }

    /// <summary>
    /// Indicate that expression evaluate to TRUE or FALSE (=, >, ...). OR and AND are not considered Predicate expressions
    /// Predicate expressions must have Left/Right expressions
    /// </summary>
    internal bool IsPredicate =>
        this.Type == BsonExpressionType.Equal ||
        this.Type == BsonExpressionType.Like ||
        this.Type == BsonExpressionType.Between ||
        this.Type == BsonExpressionType.GreaterThan ||
        this.Type == BsonExpressionType.GreaterThanOrEqual ||
        this.Type == BsonExpressionType.LessThan ||
        this.Type == BsonExpressionType.LessThanOrEqual ||
        this.Type == BsonExpressionType.NotEqual ||
        this.Type == BsonExpressionType.In;


    #endregion
}
