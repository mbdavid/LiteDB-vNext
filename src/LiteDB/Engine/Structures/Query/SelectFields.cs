namespace LiteDB.Engine;

internal struct SelectFields
{
    public static readonly SelectFields Default = new (BsonExpression.Root);

    private readonly BsonExpression _docExpr;
    private readonly IReadOnlyList<SelectField> _fields;

    public SelectFields(BsonExpression docExpr)
    {
        _docExpr = docExpr;
        _fields = Array.Empty<SelectField>();
    }

    public SelectFields(IReadOnlyList<SelectField> fields)
    {
        _docExpr = docExpr;
    }
}
