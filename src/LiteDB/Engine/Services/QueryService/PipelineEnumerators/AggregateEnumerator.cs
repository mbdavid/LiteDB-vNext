namespace LiteDB.Engine;

internal class AggregateEnumerator : IPipeEnumerator
{
    // depenrency injection
    private readonly Collation _collation;
    private readonly BsonExpression _keyExpr;

    // fields
    private readonly IPipeEnumerator _enumerator;
    private readonly List<(string key, IAggregateFunc func)> _fields = new();

    private BsonValue _currentKey = BsonValue.MinValue;

    private bool _init = false;
    private bool _eof = false;

    /// <summary>
    /// Aggregate values according aggregate functions (reduce functions). Require "ProvideDocument" from IPipeEnumerator
    /// </summary>
    public AggregateEnumerator(BsonExpression keyExpr, SelectFields fields, IPipeEnumerator enumerator, Collation collation)
    {
        _keyExpr = keyExpr;
        _enumerator = enumerator;
        _collation = collation;

        // aggregate requires key/value fields to compute (does not support single root)
        if (fields.IsSingleExpression) throw new ArgumentException($"AggregateEnumerator has no support for single expression");

        // getting aggregate expressions call
        foreach (var field in fields.Fields)
        {
            // expression must be a CALL with AggregateAttribute
            if (field.Expression is not CallBsonExpression call) continue;

            var aggrAttr = call.Method.GetCustomAttribute<AggregateAttribute>();

            if (aggrAttr is null) continue;

            // creating computed aggregate function based on BsonExpression aggregate function
            var func = Activator.CreateInstance(aggrAttr.AggregateType) as IAggregateFunc;

            if (func is null) continue;

            _fields.Add((field.Name, func));
        }

        if (_enumerator.Emit.Document == false) throw ERR($"Aggregate pipe enumerator requires document from last pipe");
    }

    public PipeEmit Emit => new(indexNodeID: false, dataBlockID: false, document: true);

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
                if (_init == false)
                {
                    _init = true;
                    _currentKey = key;
                }

                // keep running with same value
                if (_currentKey == key)
                {
                    foreach (var field in _fields)
                    {
                        field.func.Iterate(_currentKey, item.Document!, _collation);
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
        var doc = new BsonDocument
        {
            ["key"] = _currentKey,
        };

        foreach (var field in _fields)
        {
            doc[field.key] = field.func.GetResult();
            field.func.Reset();
        }

        return new PipeValue(doc);
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"AGGREGATE {_keyExpr}", deep);

        foreach (var field in _fields)
        {
            builder.Add($"{field.key} = {field.func}", deep - 1);
        }

        _enumerator.GetPlan(builder, ++deep);
    }

    public void Dispose()
    {
    }
}
