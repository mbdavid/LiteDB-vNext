namespace LiteDB.Engine;

internal struct SelectField
{
    public string Name { get; }
    public BsonExpression Expression { get; }
}
