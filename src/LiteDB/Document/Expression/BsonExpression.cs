namespace LiteDB;

public abstract partial class BsonExpression
{
    public abstract BsonExpressionType Type { get; }

    protected virtual IEnumerable<BsonExpression> Children => new BsonExpression[0];

    private string _expression = null;

    public string Expression => _expression ?? (_expression = this.ToString());

    internal abstract BsonValue Execute(BsonExpressionContext context);

    public BsonValue Execute(BsonValue root = null, BsonDocument parameters = null, Collation collation = null)
    {
        var context = new BsonExpressionContext(root, parameters, collation);

        return Execute(context);
    }

    /// <summary>
    /// Indicate that expression evaluate to TRUE or FALSE (=, >, ...). OR and AND are not considered Predicate expressions
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

    // para descobrir se é imutavel: todo mundo é, exceto algumas calls. Varrer filhas?
}
