namespace LiteDB;

internal class BsonExpressionInfo
{
    public bool HasRoot { get; }

    public IEnumerable<string> RootFields { get; }

    public bool IsImmutable { get; }

    public string Expression { get; }

    public BsonExpressionInfo(BsonExpression expr)
    {
        this.Expression = expr.ToString();

        var rootFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var isImmutable = true;
        var hasRoot = false;

        this.GetInfo(expr, rootFields, ref isImmutable, ref hasRoot);

        this.RootFields = rootFields;
        this.IsImmutable = isImmutable;
        this.HasRoot = hasRoot;
    }

    private void GetInfo(BsonExpression expr, HashSet<string> rootFields, ref bool isImmutable, ref bool hasRoot)
    {
        // if expression are path from root document, get root field
        if (expr.Type == BsonExpressionType.Path)
        {
            var path = (PathBsonExpression)expr;

            if (path.Source.Type == BsonExpressionType.Root)
            {
                rootFields.Add(path.Field);
            }
        }

        if (expr.Type == BsonExpressionType.Root)
        {
            hasRoot = true;
        }

        if (expr.Type == BsonExpressionType.Call)
        {
            var call = (CallBsonExpression)expr;

            if (call.Method.GetCustomAttribute<VolatileAttribute>() != null)
            {
                isImmutable = false;
            }
        }

        if (expr.Type == BsonExpressionType.Parameter)
        {
            isImmutable = false;
        }

        foreach(var child in expr.Children)
        {
            this.GetInfo(child, rootFields, ref isImmutable, ref hasRoot);
        }
    }
}
