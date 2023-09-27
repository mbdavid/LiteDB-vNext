namespace LiteDB.Engine;

public readonly struct SelectField
{
    public readonly string Name;
    public readonly BsonExpression Expression;

    public SelectField(string name, BsonExpression expression)
    {
        this.Name = name;
        this.Expression = expression;
    }
}
