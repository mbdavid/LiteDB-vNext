namespace LiteDB.Engine;

internal class TransformEnumerator : IPipeEnumerator<BsonDocument>
{
    private readonly Collation _collation;
    private readonly IPipeEnumerator<BsonDocument> _enumerator;

    private readonly BsonExpression _expr;

    private bool _eof = false;

    public TransformEnumerator(BsonExpression expr, IPipeEnumerator<BsonDocument> enumerator, Collation collation)
    {
        _expr = expr;
        _enumerator = enumerator;
        _collation = collation;
    }

    public async ValueTask<BsonDocument?> MoveNextAsync(IDataService dataService, IIndexService indexService)
    {
        if (_eof) return null;

        var next = await _enumerator.MoveNextAsync(dataService, indexService);

        if (next is null)
        {
            _eof = true;
            return null;
        }

        var result = _expr.Execute(next, null, _collation);

        return result.AsDocument;
    }
}
