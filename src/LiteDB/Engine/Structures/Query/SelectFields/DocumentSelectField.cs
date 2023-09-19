namespace LiteDB.Engine;

internal class DocumentSelectField : ISelectField
{
    public BsonExpression Expression { get; }

    public DocumentSelectField(BsonExpression expression)
    {
        this.Expression = expression;
    }
}
