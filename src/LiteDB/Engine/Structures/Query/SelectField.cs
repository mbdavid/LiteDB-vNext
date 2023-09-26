namespace LiteDB.Engine;

internal readonly struct SelectField
{
    public string Name { get; }
    public BsonExpression Expression { get; }

    public SelectField(string name, BsonExpression expression)
    {
        this.Name = name;
        this.Expression = expression;
    }
}
