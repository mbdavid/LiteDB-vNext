namespace LiteDB.Engine;

unsafe internal class IndexInEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;
    private readonly IndexDocument _indexDocument;

    private readonly BsonValue[] _values;
    private int _valueIndex = 0;

    private bool _init = false;
    private bool _eof = false;

    private IndexEqualsEnumerator? _currentIndex;

    public IndexInEnumerator(
        IEnumerable<BsonValue> values,
        IndexDocument indexDocument,
        Collation collation)
    {
        _values = values.Distinct().ToArray();
        _indexDocument = indexDocument;
        _collation = collation;
    }

    public PipeEmit Emit => new(true, true, false);

    public unsafe PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        // in first run, gets head node
        if (_init == false)
        {
            _init = true;

            var first = _values[_valueIndex];
            
            _currentIndex = new IndexEqualsEnumerator(first, _indexDocument, _collation);
            
            return _currentIndex.MoveNext(context);
        }
        else
        {
            var pipeValue = _currentIndex!.MoveNext(context);

            if (pipeValue.IsEmpty)
            {
                _valueIndex++;

                if (_valueIndex == _values.Length)
                {
                    _eof = true;
                    return PipeValue.Empty;
                }

                var value = _values[_valueIndex];

                _currentIndex = new IndexEqualsEnumerator(value, _indexDocument, _collation);

                return _currentIndex.MoveNext(context);
            }

            return pipeValue;
        }
    }

    public void Dispose()
    {
    }
}