namespace LiteDB.Engine;

public readonly struct SelectField
{
    private static Dictionary<string, Func<BsonExpression, IAggregateFunc>> _aggregateMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        ["COUNT"] = (e) => new CountFunc(e),
    };

    public readonly string Name;
    public readonly BsonExpression Expression;

    public SelectField(string name, BsonExpression expression)
    {
        this.Name = name;
        this.Expression = expression;
    }

    public bool IsAggregate =>
        this.Expression is CallBsonExpression call &&
         _aggregateMethods.ContainsKey(call.Method.Name);

    public IAggregateFunc CreateAggregateFunc()
    {
        var call = this.Expression as CallBsonExpression;

        var fn = _aggregateMethods[call!.Method.Name];
        var expr = call.Children.FirstOrDefault();

        return fn(expr);
    }
}
