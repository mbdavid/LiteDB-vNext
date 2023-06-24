namespace LiteDB.Engine;

[AutoInterface]
internal class FilterEnumerator : IFilterEnumerator
{
    private readonly Collation _collation;
    private readonly List<BsonExpression> _filters;

    private readonly IIndexEnumerator _indexEnumerator;

    private bool _eof = false;

    public FilterEnumerator(List<BsonExpression> filters)
    {
        _filters = filters;
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
