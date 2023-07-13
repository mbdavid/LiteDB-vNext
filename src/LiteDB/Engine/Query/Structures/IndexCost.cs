namespace LiteDB.Engine;

/// <summary>
/// Calculate index cost based on expression/collection index. 
/// Lower cost is better - lowest will be selected
/// </summary>
internal struct IndexCost
{
    private readonly int _cost;
    private int _order;
    private readonly BsonExpressionType _exprType;
    private readonly BsonValue _value;
    private readonly BsonDocument _queryParameters;
    private readonly IndexDocument _indexDocument;
    private readonly Collation _collation;

    public int Cost => _cost;

    public int Order { get => _order; set => _order = value; }

    public IndexCost(
        IndexDocument indexDocument, 
        BinaryBsonExpression expression, 
        int order, 
        BsonDocument queryParameters,
        Collation collation)
    {
        _collation = collation;

        _indexDocument = indexDocument;
        _order = order;
        _queryParameters = queryParameters;

        if (expression.Left == indexDocument.Expression)
        {
            _exprType = expression.Type switch
            {
                BsonExpressionType.GreaterThan => BsonExpressionType.LessThan,
                BsonExpressionType.GreaterThanOrEqual => BsonExpressionType.LessThanOrEqual,
                BsonExpressionType.LessThan => BsonExpressionType.GreaterThan,
                BsonExpressionType.LessThanOrEqual => BsonExpressionType.GreaterThanOrEqual,
                _ => expression.Type
            };

            _value = expression.Left.Execute(null, _queryParameters, _collation);
        }
        else
        {
            _exprType = expression.Type;
            _value = expression.Right.Execute(null, _queryParameters, _collation);
        }

        // calcs index cost (lower is best)
        _cost = (_exprType, indexDocument.Unique) switch
        {
            (BsonExpressionType.Equal, true) => 1,
            (BsonExpressionType.Equal, false) => 10,
            (BsonExpressionType.In, _) => 20,
            (BsonExpressionType.GreaterThan, _) => 50,
            (BsonExpressionType.GreaterThanOrEqual, _) => 50,
            (BsonExpressionType.LessThan, _) => 50,
            (BsonExpressionType.LessThanOrEqual, _) => 50,
            (BsonExpressionType.Between, _) => 50,
            (BsonExpressionType.NotEqual, _) => 80,
            (_, _) => 100
        };
    }

    public IPipeEnumerator CreateIndex()
    {
        return _exprType switch
        {
            BsonExpressionType.Equal => new IndexEqualsEnumerator(_value, _indexDocument, _collation),
            BsonExpressionType.Between => new IndexRangeEnumerator(_value.AsArray[0], _value.AsArray[1], true, true, _order, _indexDocument, _collation),
            BsonExpressionType.GreaterThan => new IndexRangeEnumerator(_value, BsonValue.MaxValue, false, true, _order, _indexDocument, _collation),
            BsonExpressionType.GreaterThanOrEqual => new IndexRangeEnumerator(_value, BsonValue.MaxValue, true, true, _order, _indexDocument, _collation),
            BsonExpressionType.LessThan => new IndexRangeEnumerator(BsonValue.MinValue, _value, false, true, _order, _indexDocument, _collation),
            BsonExpressionType.LessThanOrEqual => new IndexRangeEnumerator(BsonValue.MinValue, _value, true, true, _order, _indexDocument, _collation),
            _ => throw new NotSupportedException()
        };
    }

}