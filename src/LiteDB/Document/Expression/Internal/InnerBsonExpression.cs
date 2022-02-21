namespace LiteDB;

internal class InnerBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Inner;

    internal override IEnumerable<BsonExpression> Children => new[] { this.InnerExpression };

    public BsonExpression InnerExpression { get; }

    public InnerBsonExpression(BsonExpression inner)
    {
        this.InnerExpression = inner;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        return this.InnerExpression.Execute(context);
    }

    public override string ToString()
    {
        return "(" + this.InnerExpression.ToString() + ")";
    }
}
