namespace LiteDB.Engine;

internal readonly struct SelectFields
{
    /// <summary>
    /// SELECT *
    /// </summary>
    public static readonly SelectFields Root = new (BsonExpression.Root);

    /// <summary>
    /// SELECT $._id
    /// </summary>
    public static readonly SelectFields Id = new(new SelectField[] { new SelectField("_id", BsonExpression.Id) });

    // fields
    private readonly BsonExpression _docExpr;
    private readonly IReadOnlyList<SelectField> _fields;

    public SelectFields(BsonExpression docExpr)
    {
        _docExpr = docExpr;
        _fields = Array.Empty<SelectField>();
    }

    public SelectFields(IReadOnlyList<SelectField> fields)
    {
        _docExpr = BsonExpression.Empty;
        _fields = fields;
    }
}
