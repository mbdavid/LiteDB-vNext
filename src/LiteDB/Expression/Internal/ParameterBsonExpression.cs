namespace LiteDB;

internal class ParameterBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Parameter;

    public string Name { get; }

    public ParameterBsonExpression(string name)
    {
        this.Name = name;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        return context.Parameters[this.Name];
    }

    public override string ToString()
    {
        return "@" + this.Name;
    }
}
