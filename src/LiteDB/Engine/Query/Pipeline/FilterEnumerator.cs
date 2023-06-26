namespace LiteDB.Engine;

internal class FilterEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;
    private readonly IList<BsonExpression> _filters;

    private readonly IPipeEnumerator _enumerator;

    private bool _eof = false;

    public FilterEnumerator(IList<BsonExpression> filters, IPipeEnumerator enumerator, Collation collation)
    {
        _filters = filters;
        _enumerator = enumerator;
        _collation = collation;
    }

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        while (!_eof)
        {
            var item = await _enumerator.MoveNextAsync(context);

            if (item.Eof)
            {
                _eof = true;
            }
            else
            {
                foreach(var filter in _filters)
                {
                    var result = filter.Execute(item.Value, null, _collation);

                    if (result.IsBoolean && result.AsBoolean)
                    {
                        return item;
                    }
                }
            }
        }

        return PipeValue.Empty;
    }
}
