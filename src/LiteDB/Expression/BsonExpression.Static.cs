namespace LiteDB;

public abstract partial class BsonExpression
{
    private static readonly ConcurrentDictionary<string, BsonExpression> _cache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly BsonExpression _root = new ScopeBsonExpression(true);
    private static readonly BsonExpression _current = new ScopeBsonExpression(false);

    public static BsonExpression Constant(BsonValue value) => new ConstantBsonExpression(value);

    public static BsonExpression Parameter(string name) => new ParameterBsonExpression(name);

    public static BsonExpression Root() => _root;

    public static BsonExpression Current() => _current;

    public static BsonExpression Path(BsonExpression source, string field) => new PathBsonExpression(source, field);

    public static BsonExpression Map(BsonExpression source, BsonExpression selector) => new MapBsonExpression(source, selector);

    public static BsonExpression Filter(BsonExpression source, BsonExpression selector) => new FilterBsonExpression(source, selector);

    public static BsonExpression ArrayIndex(BsonExpression array, BsonExpression index) => new ArrayIndexBsonExpression(array, index);

    public static BsonExpression MakeArray(IEnumerable<BsonExpression> items) => new MakeArrayBsonExpression(items);

    public static BsonExpression MakeDocument(IDictionary<string, BsonExpression> values) => new MakeDocumentBsonExpression(values);

    public static BsonExpression Inner(BsonExpression inner) => new InnerBsonExpression(inner);

    public static BsonExpression Call(MethodInfo method, BsonExpression[] parameters) => new CallBsonExpression(method, parameters);

    #region BinaryBsonExpressions

    public static BsonExpression Add(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.Add, left, right);

    public static BsonExpression Subtract(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.Subtract, left, right);

    public static BsonExpression Multiply(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.Multiply, left, right);

    public static BsonExpression Divide(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.Divide, left, right);

    public static BsonExpression Modulo(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.Modulo, left, right);


    public static BsonExpression Equal(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.Equal, left, right);

    public static BsonExpression NotEqual(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.NotEqual, left, right);

    public static BsonExpression GreaterThan(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.GreaterThan, left, right);

    public static BsonExpression GreaterThanOrEqual(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.GreaterThanOrEqual, left, right);

    public static BsonExpression LessThan(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.LessThan, left, right);

    public static BsonExpression LessThanOrEqual(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.LessThanOrEqual, left, right);

    public static BsonExpression Contains(BsonExpression array, BsonExpression item) => new BinaryBsonExpression(BsonExpressionType.Contains, array, item);

    public static BsonExpression Between(BsonExpression value, BsonExpression start, BsonExpression end) => new BinaryBsonExpression(BsonExpressionType.Between, value, MakeArray(new[] { start, end }));

    internal static BsonExpression Between(BsonExpression value, BsonExpression array) => new BinaryBsonExpression(BsonExpressionType.Between, value, array);

    public static BsonExpression Like(BsonExpression str, BsonExpression item) => new BinaryBsonExpression(BsonExpressionType.Like, str, item);

    public static BsonExpression In(BsonExpression item, BsonExpression array) => new BinaryBsonExpression(BsonExpressionType.In, item, array);

    public static BsonExpression Or(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.Or, left, right);

    public static BsonExpression And(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.And, left, right);

    #endregion

    public static BsonExpression Create(string expr)
    {
        return _cache.GetOrAdd(expr, e => Create(new Tokenizer(e)));
    }

    internal static BsonExpression Create(Tokenizer tokenizer)
    {
        return BsonExpressionParser.ParseFullExpression(tokenizer, true);
    }

    #region MethodCall quick access

    /// <summary>
    /// Get all registered methods for BsonExpressions
    /// </summary>
    public static IEnumerable<MethodInfo> Methods => _methods.Values;

    /// <summary>
    /// Load all static methods from BsonExpressionMethods class. Use a dictionary using name + parameter count
    /// </summary>
    private static readonly Dictionary<string, MethodInfo> _methods =
        typeof(BsonExpressionMethods).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .ToDictionary(m => m.Name.ToUpper() + "~" + m.GetParameters().Where(p => p.ParameterType != typeof(Collation)).Count());

    /// <summary>
    /// Get expression method with same name and same parameter - return null if not found
    /// </summary>
    internal static MethodInfo? GetMethod(string name, int parameterCount)
    {
        var key = name.ToUpper() + "~" + parameterCount;

        if (_methods.TryGetValue(key, out var method))
        {
            return method;
        }

        return default;
    }

    #endregion
}
