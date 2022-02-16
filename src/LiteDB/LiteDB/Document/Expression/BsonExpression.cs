namespace LiteDB;

public abstract partial class BsonExpression
{
    public abstract BsonExpressionType Type { get; }

    internal abstract BsonValue Execute(BsonExpressionContext context);

    public BsonValue Execute(BsonValue root = null, BsonDocument parameters = null, Collation collation = null)
    {
        var context = new BsonExpressionContext(root, parameters, collation);

        return Execute(context);
    }

    // para descobrir se é imutavel: todo mundo é, exceto algumas calls. Varrer filhas?
}
