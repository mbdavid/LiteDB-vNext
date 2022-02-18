namespace LiteDB;

internal class ScopeBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => this.IsRoot ? BsonExpressionType.Root : BsonExpressionType.Current;

    private bool IsRoot { get; }

    public ScopeBsonExpression(bool root)
    {
        this.IsRoot = root;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        return this.IsRoot ? context.Root : context.Current;
    }

    public override string ToString()
    {
        return this.IsRoot ? "$" : "@";
    }
}
