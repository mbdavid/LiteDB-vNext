namespace LiteDB;

internal class MakeDocumentBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Document;

    protected override IEnumerable<BsonExpression> Children => this.Values.Values;

    public IDictionary<string, BsonExpression> Values { get; }

    public MakeDocumentBsonExpression(IDictionary<string, BsonExpression> values)
    {
        this.Values = values;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        return new BsonDocument(this.Values.ToDictionary(x => x.Key, x => x.Value.Execute(context)));
    }

    public override string ToString()
    {
        return "{" + String.Join(",", this.Values.Select(x => 
            (x.Key.IsWord() ? x.Key : $"\"{x.Key}\"") + ":" +
            x.Value.ToString())) + "}";
    }
}
