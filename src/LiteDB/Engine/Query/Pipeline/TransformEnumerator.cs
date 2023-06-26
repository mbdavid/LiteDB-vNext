namespace LiteDB.Engine;

[AutoInterface(typeof(IPipeEnumerator))]
internal class TransformEnumerator : ITransformEnumerator
{
    private readonly Collation _collation;
    private readonly IPipeEnumerator _enumerator;

    private readonly BsonExpression _expr;

    private bool _eof = false;

    public TransformEnumerator(BsonExpression expr, IPipeEnumerator enumerator, Collation collation)
    {
        _expr = expr;
        _enumerator = enumerator;
        _collation = collation;
    }

    public async ValueTask<BsonDocument?> MoveNextAsync(IDataService dataService, IIndexService indexService)
    {
        if (_eof) return null;

        var doc = await _enumerator.MoveNextAsync(dataService, indexService);

        if (doc is null)
        {
            _eof = true;
            return null;
        }

        var result = _expr.Execute(doc, null, _collation);

        return result.AsDocument;
    }
}
