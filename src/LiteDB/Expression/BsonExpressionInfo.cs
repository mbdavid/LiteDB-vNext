namespace LiteDB;

internal class BsonExpressionInfo
{
    /// <summary>
    /// Indicate that expression contains a single $ root (should load full document)
    /// </summary>
    public bool HasRoot { get; }

    /// <summary>
    /// Get root fields keys used in document
    /// </summary>
    public string[] RootFields { get; }

    /// <summary>
    /// Indicate that this expression can result a diferent result for a same input arguments
    /// </summary>
    public bool IsVolatile { get; }

    /// <summary>
    /// Get some expression infos reading full expression tree
    /// </summary>
    public BsonExpressionInfo(BsonExpression expr)
    {
        var rootFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var isVolatile = false;
        var hasRoot = false;

        this.GetInfo(expr, rootFields, ref isVolatile, ref hasRoot);

        this.RootFields = rootFields.ToArray();
        this.IsVolatile = isVolatile;
        this.HasRoot = hasRoot;
    }

    private void GetInfo(BsonExpression expr, HashSet<string> rootFields, ref bool isVolatile, ref bool hasRoot)
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

            if (call.IsVolatile == true)
            {
                isVolatile = true;
            }
        }

        if (expr.Type == BsonExpressionType.Parameter)
        {
            isVolatile = true;
        }

        foreach(var child in expr.Children)
        {
            this.GetInfo(child, rootFields, ref isVolatile, ref hasRoot);
        }
    }
}
