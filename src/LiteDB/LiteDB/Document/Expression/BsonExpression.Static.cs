namespace LiteDB;

public abstract partial class BsonExpression
{
    public static BsonExpression Constant(BsonValue value) => new ConstantBsonExpression(value);

    public static BsonExpression Parameter(string name) => new ParameterBsonExpression(name);

    public static BsonExpression Root() => new PathBsonExpression(null, true);

    public static BsonExpression Path(string field, bool root = true) => new PathBsonExpression(field, root);

    public static BsonExpression Path(BsonExpression source, string field) => new PathBsonExpression(source, field);

    public static BsonExpression Map(BsonExpression source, BsonExpression selector) => new MapBsonExpression(source, selector);

    public static BsonExpression Filter(BsonExpression source, BsonExpression selector) => new FilterBsonExpression(source, selector);

    public static BsonExpression ArrayIndex(BsonExpression array, BsonExpression index) => new ArrayIndexBsonExpression(array, index);

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

    public static BsonExpression Or(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.Or, left, right);

    public static BsonExpression And(BsonExpression left, BsonExpression right) => new BinaryBsonExpression(BsonExpressionType.And, left, right);

    #endregion

    public static BsonExpression Create(string expr)
    {
        var tokenizer = new Tokenizer(expr);

        return BsonExpressionParser.ParseFullExpression(tokenizer, true);
    }
}
