namespace LiteDB;

internal class FilterBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Map;

    public BsonExpression Source { get; }

    public BsonExpression Selector { get; }

    public FilterBsonExpression(BsonExpression source, BsonExpression selector)
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

                if (value.IsBoolean && value.AsBoolean)
                {
                    yield return item;
                }

                context.Current = context.Root;
            }
        };

        return new BsonArray(source());
    }

    public override string ToString()
    {
        var filter = this.Selector.ToString();

        // if filter always returns true, use * $.items[*]
        if (filter == "true") filter = "*";

        return this.Source.ToString() + "[" + filter + "]";
    }
}
