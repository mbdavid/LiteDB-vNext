namespace LiteDB.Engine;

[AutoInterface(typeof(IPipeEnumerator))]
internal class FilterEnumerator : IFilterEnumerator
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

    public async ValueTask<BsonDocument?> MoveNextAsync(IDataService dataService, IIndexService indexService)
    {
        while (!_eof)
        {
            var doc = await _enumerator.MoveNextAsync(dataService, indexService);

            if (doc is null)
            {
                _eof = true;
            }
            else if (_filters.Count == 0) // by-pass
            {
                return doc;
            }
            else
            {
                foreach(var filter in _filters)
                {
                    var result = filter.Execute(doc, null, _collation);

                    if (result.IsBoolean && result.AsBoolean)
                    {
                        return doc;
                    }
                }
            }
        }

        return null;
    }
}
