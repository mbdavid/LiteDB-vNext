namespace LiteDB;

internal class PathBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Path;

    private readonly bool _root;

    public string Field { get; }

    public BsonExpression Source { get; }

    public string RootField => _root ? this.Field : null;

    public PathBsonExpression(string field, bool root = true)
    {
        _root = root;

        this.Field = field;
        this.Source = null;
    }

    public PathBsonExpression(BsonExpression source, string field)
    {
        _root = true;

        this.Field = field;
        this.Source = source;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        var source = _root ? this.Source?.Execute(context) ?? context.Root : context.Current;

        if (this.Field == null) return source;

        if (source.IsDocument)
        {
            return source.AsDocument[this.Field];
        }
        else
        {
            return BsonValue.Null;
        }
    }

    public override string ToString()
    {
        if (this.Source == null)
        {
            var sb = new StringBuilder(_root ? "$" : "@");

            if (this.Field != null)
            {
                var content = this.Field.IsWord() ?
                    this.Field :
                    "[\"" + this.Field + "\"]";

                sb.Append("." + content);
            }

            return sb.ToString();
        }

        return this.Source.ToString() + "." + this.Field;
    }
}
