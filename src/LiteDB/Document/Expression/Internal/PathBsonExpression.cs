namespace LiteDB;

internal class PathBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Path;

    public string Field { get; }

    public BsonExpression Source { get; }

    public PathBsonExpression(BsonExpression source, string field)
    {
        this.Field = field;
        this.Source = source;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        var source = this.Source.Execute(context);

        if (this.Field == null) return source;

        if (source.IsDocument)
        {
            // return document field (or null if not exists)
            return source.AsDocument[this.Field];
        }
        else if (source.IsArray)
        {
            // returns document fields inside array (only for sub documents)
            return new BsonArray(source.AsArray.Select(x => x.IsDocument ? x.AsDocument[this.Field] : BsonValue.Null));
        }
        else
        {
            return BsonValue.Null;
        }
    }

    public override string ToString()
    {
        var field = this.Field.IsWord() ?
            this.Field :
            "[\"" + this.Field + "\"]";

        return this.Source.ToString() + "." + field;
    }
}
