namespace LiteDB.Engine;

/// <summary>
/// </summary>
public class CountFunc : IAggregateFunc
{
    private readonly string _name;
    private readonly BsonExpression _expr;

    private int _count;

    public CountFunc(string name, BsonExpression expr)
    {
        _name = name;
        _expr = expr;
    }

    public string Name => _name;
    public BsonExpression Expression => _expr;

    public void Iterate(BsonValue key, BsonDocument document, Collation collation)
    {
        var result = _expr.Execute(document, null, collation);

        if (!result.IsNull) _count++;
    }

    public BsonValue GetResult()
    {
        return _count;
    }

    public void Reset()
    {
        _count = 0;
    }

    public override string ToString()
    {
        return $"COUNT({_expr})";
    }
}