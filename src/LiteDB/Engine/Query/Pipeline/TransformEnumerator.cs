namespace LiteDB.Engine;

internal class TransformEnumerator : IPipeEnumerator
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

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var item = await _enumerator.MoveNextAsync(context);

        if (item.Eof)
        {
            _eof = true;
            return PipeValue.Empty;
        }

        var result = _expr.Execute(item.Value, null, _collation);

        return new PipeValue(item.RowID, result.AsDocument);
    }
}
