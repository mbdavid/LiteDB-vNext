namespace LiteDB.Engine;

internal class TransformEnumerator : IPipeEnumerator
{
    private readonly BsonExpression _expr;
    private readonly Collation _collation;
    private readonly IPipeEnumerator _enumerator;

    private bool _eof = false;

    public TransformEnumerator(BsonExpression expr, Collation collation, IPipeEnumerator enumerator)
    {
        _expr = expr;
        _enumerator = enumerator;
        _collation = collation;

        if (_enumerator.Emit.Document == false) throw ERR($"Transform pipe enumerator requires document from last pipe");
    }

    public PipeEmit Emit => new (_enumerator.Emit.IndexNodeID, _enumerator.Emit.DataBlockID, true);

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var item = await _enumerator.MoveNextAsync(context);

        if (item.IsEmpty)
        {
            _eof = true;
            return PipeValue.Empty;
        }

        var result = _expr.Execute(item.Document, context.QueryParameters, _collation);

        return new PipeValue(item.IndexNodeID, item.DataBlockID, result.AsDocument);
    }

    public void Dispose()
    {
    }
}
