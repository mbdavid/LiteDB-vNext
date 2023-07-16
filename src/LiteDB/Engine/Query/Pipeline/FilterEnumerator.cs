namespace LiteDB.Engine;

internal class FilterEnumerator : IPipeEnumerator
{
    private readonly BsonExpression _filter;
    private readonly Collation _collation;
    private readonly IPipeEnumerator _enumerator;

    private bool _eof = false;

    public FilterEnumerator(BsonExpression filter, Collation collation, IPipeEnumerator enumerator)
    {
        _filter = filter;
        _enumerator = enumerator;
        _collation = collation;
    }

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        while (!_eof)
        {
            var item = await _enumerator.MoveNextAsync(context);

            if (item.IsEmpty)
            {
                _eof = true;
            }
            else
            {
                var result = _filter.Execute(item.Document, context.QueryParameters, _collation);

                if (result.IsBoolean && result.AsBoolean)
                {
                    return item;
                }
            }
        }

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
