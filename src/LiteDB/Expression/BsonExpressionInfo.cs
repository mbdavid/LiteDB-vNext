namespace LiteDB;

internal readonly struct BsonExpressionInfo
{
    /// <summary>
    /// Indicate that expression contains a root $ but without any path navigation (should load full document)
    /// </summary>
    public bool FullRoot { get; }

    /// <summary>
    /// Get root fields keys used in document (empty array if no fields found)
    /// </summary>
    public string[] RootFields { get; }

    /// <summary>
    /// Indicate that this expression can result a diferent result for a same input arguments
    /// </summary>
    public bool IsVolatile { get; }

    /// <summary>
    /// Check if this expression can be used in index expression (contains no paramter or volatile method calls)
    /// </summary>
    public bool IsIndexable { get; }

    /// <summary>
    /// Returns if this expression (or any child) has use of any aggregate call (COUNT, MIN, MAX, ...)
    /// </summary>
    public bool HasAggregateCall { get; }

    /// <summary>
    /// Return if this expression contains @ parameters
    /// </summary>
    public bool HasParameter { get; }

    /// <summary>
    /// Return  is this expression (or any children) access the root document
    /// </summary>
    public bool HasDocumentAccess { get; }

    /// <summary>
    /// Returns if this expression (or any children) contains expression using source *
    /// Like `COUNT(*)`, `MAX(*._id)`
    /// </summary>
    public bool UseSource { get; }

    /// <summary>
    /// Get some expression infos reading full expression tree
    /// </summary>
    public BsonExpressionInfo(BsonExpression expr)
    {
        var rootFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var isVolatile = false;
        var fullRoot = false;

        this.GetInfo(expr, rootFields, ref isVolatile, ref fullRoot);

        this.RootFields = rootFields.ToArray();
        this.IsVolatile = isVolatile;
        this.FullRoot = fullRoot;
    }

    private void GetInfo(BsonExpression expr, HashSet<string> rootFields, ref bool isVolatile, ref bool fullRoot)
    {
        // get root fields from path
        if (expr.Type == BsonExpressionType.Path)
        {
            var path = (PathBsonExpression)expr;

            if (path.Source.Type == BsonExpressionType.Root)
            {
                rootFields.Add(path.Field);

                // avoid enter on path children
                return;
            }
        }
        // $ root sign with no path navigation
        else if (expr.Type == BsonExpressionType.Root)
        {
            fullRoot = true;
        }
        // call methods mark as [Volatile]
        else if (expr.Type == BsonExpressionType.Call)
        {
            var call = (CallBsonExpression)expr;

            if (call.IsVolatile == true)
            {
                isVolatile = true;
            }
        }
        // parameters are volatile
        else if (expr.Type == BsonExpressionType.Parameter)
        {
            isVolatile = true;
        }

        // apply for all children recursive
        foreach(var child in expr.Children)
        {
            this.GetInfo(child, rootFields, ref isVolatile, ref fullRoot);
        }
    }
}
