namespace LiteDB.Engine;

internal class IncludeEnumerator : IPipeEnumerator
{
    private readonly BsonExpression _pathExpr;
    private readonly Collation _collation;
    private readonly IPipeEnumerator _enumerator;

    private bool _init = false;
    private bool _eof = false;

    public IncludeEnumerator(BsonExpression pathExpr, Collation collation, IPipeEnumerator enumerator)
    {
        _pathExpr = pathExpr;
        _collation = collation;
        _enumerator = enumerator;
    }

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        while (!_eof)
        {
            var item = await _enumerator.MoveNextAsync(context);

            if (item.IsEmpty)
            {
                _init = _eof = true;
            }
            else
            {
                // initialize current key with first key
                if (!_init)
                {
                    _init = true;

                }

            }
        }

    }

    public void Dispose()
    {
    }
}
