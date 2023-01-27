namespace LiteDB;

internal class MapBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Map;

    internal override IEnumerable<BsonExpression> Children => new[] { this.Source, this.Selector };

    public BsonExpression Source { get; }

    public BsonExpression Selector { get; }

    public MapBsonExpression(BsonExpression source, BsonExpression selector)
    {
        this.Source = source;
        this.Selector = selector;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        IEnumerable<BsonValue> source()
        {
            var src = this.Source.Execute(context);

            if (!src.IsArray) yield break;

            foreach(var item in src.AsArray)
            {
                context.Current = item;

                var value = this.Selector.Execute(context);

                yield return value;

                context.Current = context.Root;
            }
        };

        return new BsonArray(source());
    }

    public override string ToString()
    {
        return this.Source.ToString() + "=>" + this.Selector.ToString();
    }
}
