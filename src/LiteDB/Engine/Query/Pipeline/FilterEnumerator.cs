namespace LiteDB.Engine;

internal class FilterEnumerator : IPipeEnumerator<BsonDocument>
{
    private readonly Collation _collation;
    private readonly IList<BsonExpression> _filters;

    private readonly IPipeEnumerator<BsonDocument> _enumerator;

    private bool _eof = false;

    public FilterEnumerator(IList<BsonExpression> filters, IPipeEnumerator<BsonDocument> enumerator, Collation collation)
    {
        _filters = filters;
        _enumerator = enumerator;
        _collation = collation;
    }

    public async ValueTask<BsonDocument?> MoveNextAsync(IDataService dataService, IIndexService indexService)
    {
        while (!_eof)
        {
            var next = await _enumerator.MoveNextAsync(dataService, indexService);

            if (next is null)
            {
                _eof = true;
            }
            else if (_filters.Count == 0) // by-pass
            {
                return next;
            }
            else
            {
                foreach(var filter in _filters)
                {
                    var result = filter.Execute(next, null, _collation);

                    if (result.IsBoolean && result.AsBoolean)
                    {
                        return next;
                    }
                }
            }
        }

        return null;
    }
}
