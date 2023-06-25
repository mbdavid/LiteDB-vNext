namespace LiteDB.Engine;

[AutoInterface(typeof(IPipelineEnumerator))]
internal class FilterEnumerator : IFilterEnumerator
{
    private readonly Collation _collation;
    private readonly IReadOnlyCollection<BsonExpression> _filters;

    private readonly IIndexEnumerator _indexEnumerator;

    private bool _eof = false;

    public FilterEnumerator(IReadOnlyCollection<BsonExpression> filters, IIndexEnumerator indexEnumerator, Collation collation)
    {
        _filters = filters;
        _indexEnumerator = indexEnumerator;
        _collation = collation;
    }

    public async ValueTask<BsonDocument?> MoveNextAsync(ITransaction transacion, IServicesFactory factory)
    {
        while (!_eof)
        {
            var doc = await _indexEnumerator.MoveNextAsync(transacion, factory);

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
