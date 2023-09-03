namespace LiteDB.Engine;

internal class AggregateEnumerator : IPipeEnumerator
{
    // depenrency injection
    private readonly Collation _collation;
    private readonly BsonExpression _keyExpr;

    // fields
    private readonly IAggregateFunc[] _funcs;
    private readonly IPipeEnumerator _enumerator;

    private BsonValue _currentKey = BsonValue.MinValue;

    private bool _init = false;
    private bool _eof = false;

    /// <summary>
    /// Aggregate values according aggregate functions (reduce functions). Require "ProvideDocument" from IPipeEnumerator
    /// </summary>
    public AggregateEnumerator(BsonExpression keyExpr, IAggregateFunc[] funcs, IPipeEnumerator enumerator, Collation collation)
    {
        _keyExpr = keyExpr;
        _funcs = funcs;
        _enumerator = enumerator;
        _collation = collation;

        if (_enumerator.Emit.Document == false) throw ERR($"Aggregate pipe enumerator requires document from last pipe");
    }

    public PipeEmit Emit => new(false, false, true);

    public PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        while (!_eof)
        {
            var item = _enumerator.MoveNext(context);

            if (item.IsEmpty)
            {
                _init = _eof = true;
            }
            else
            {
                var key = _keyExpr.Execute(item.Document, context.QueryParameters, _collation);

                // initialize current key with first key
                if (!_init)
                {
                    _init = true;
                    _currentKey = key;
                }

                // keep running with same value
                if (_currentKey == key)
                {
                    foreach (var func in _funcs)
                    {
                        func.Iterate(_currentKey, item.Document!, _collation);
                    }
                }
                // if key changes, return results in a new document
                else
                {
                    _currentKey = key;

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

        return new PipeValue(RowID.Empty, RowID.Empty, doc);
    }

    public void Dispose()
    {
    }
}
