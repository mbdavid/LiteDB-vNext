namespace LiteDB.Engine;

internal class IndexInEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly IndexDocument _indexDocument;
    private readonly BsonArray _values;

    private bool _init = false;
    private bool _eof = false;

    private int _index = -1;

    private IndexEqualsEnumerator _currentIdexer;

    public IndexInEnumerator(
        BsonArray values,
        IndexDocument indexDocument,
        Collation collation)
    {
        _values = values;
        _indexDocument = indexDocument;
        _collation = collation;
    }

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        // in first run, gets head node
        if (!_init)
        {
            _init = true;
            _index++;
            var value = _values.Distinct().ElementAtOrDefault(_index);
            if(value.IsNull)
            {
                _eof = true;
                return PipeValue.Empty;
            }
            _currentIdexer = new IndexEqualsEnumerator(value, _indexDocument, _collation);

            return await _currentIdexer.MoveNextAsync(context);
        }
        else
        {
            var pipeValue = await _currentIdexer.MoveNextAsync(context);
            if(pipeValue.IsEmpty) _init = false;
            return pipeValue;
        }
    }

    public void Dispose()
    {
    }
}