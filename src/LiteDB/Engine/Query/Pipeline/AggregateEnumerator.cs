namespace LiteDB.Engine;

internal class AggregateEnumerator : IPipeEnumerator
{
    // depenrency injection
    private readonly Collation _collation;

    // fields
    private readonly IAggregateFunc[] _funcs;
    private readonly IPipeEnumerator _enumerator;

    private BsonValue _currentKey = BsonValue.MinValue;

    private bool _init = false;
    private bool _eof = false;

    /// <summary>
    /// Aggregate values according aggregate functions (reduce functions). Require "ProvideDocument" from IPipeEnumerator
    /// </summary>
    public AggregateEnumerator(IAggregateFunc[] funcs, IPipeEnumerator enumerator, Collation collation)
    {
        _funcs = funcs;
        _enumerator = enumerator;
        _collation = collation;

        if (_enumerator.Emit.Document == false) throw ERR($"Aggregate pipe enumerator requires document from last pipe");
    }

    public PipeEmit Emit => new(false, true);

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
