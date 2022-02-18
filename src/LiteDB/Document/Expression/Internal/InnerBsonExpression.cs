namespace LiteDB;

internal class InnerBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Inner;

    public BsonExpression Inner { get; }

    public InnerBsonExpression(BsonExpression inner)
    {
        this.Inner = inner;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        return this.Inner.Execute(context);
    }

    public override string ToString()
    {
        return "(" + this.Inner.ToString() + ")";
    }
}
