namespace LiteDB.Engine;

internal class AggregateEnumerator : IPipeEnumerator
{
    private readonly IAggregateFunc[] _funcs;
    private readonly Collation _collation;
    private readonly IPipeEnumerator _enumerator;

    private BsonValue _currentKey = BsonValue.MinValue;

    private bool _init = false;
    private bool _eof = false;

    public AggregateEnumerator(IAggregateFunc[] funcs, Collation collation, IPipeEnumerator enumerator)
    {
        _funcs = funcs;
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
                    _currentKey = item.Document!;
                }

                // keep running with same value
                if (_currentKey == item.Document!)
                {
                    foreach (var func in _funcs)
                    {
                        func.Iterate(_currentKey, item.Document!, _collation);
                    }
                }
                // if key changes, return results in a new document
                else
                {
                    _currentKey = item.Document!;

                    return this.GetResults();
                }
            }
        }

        return this.GetResults();
    }

    /// <summary>
    /// Get all results from all aggregate functions and transform into a document
    /// </summary>
    private PipeValue GetResults()
    {
        var doc = new BsonDocument();

        foreach (var func in _funcs)
        {
            doc[func.Name] = func.GetResult();
            func.Reset();
        }

        return new PipeValue(PageAddress.Empty, doc);
    }

    public void Dispose()
    {
    }
}
