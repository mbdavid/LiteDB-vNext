namespace LiteDB;

internal class MakeArrayBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Array;

    public IEnumerable<BsonExpression> Items { get; }

    public MakeArrayBsonExpression(IEnumerable<BsonExpression> items)
    {
        this.Items = items;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        return new BsonArray(this.Items.Select(x => x.Execute(context)));
    }

    public override string ToString()
    {
        return "[" + String.Join(",", this.Items.Select(x => x.ToString())) + "]";  
    }
}
