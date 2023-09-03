namespace LiteDB.Engine;

internal class IndexInEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;
    private readonly IndexDocument _indexDocument;
    private readonly IList<IndexKey> _values;

    private bool _init = false;
    private bool _eof = false;

    private int _index = -1;

    private IndexEqualsEnumerator? _currentIndex;

    public IndexInEnumerator(
        IList<IndexKey> values,
        IndexDocument indexDocument,
        Collation collation)
    {
        _values = values;
        _indexDocument = indexDocument;
        _collation = collation;
    }

    public PipeEmit Emit => new(true, true, false);

    public unsafe PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        // in first run, gets head node
        if (!_init)
        {
            _init = true;
            _index++;

            var value = _values.Distinct().ElementAtOrDefault(_index);

            throw new NotImplementedException(); ;
            //if(value is null)
            //{
            //    _eof = true;
            //
            //    return PipeValue.Empty;
            //}

            _currentIndex = new IndexEqualsEnumerator(value, _indexDocument, _collation);

            return _currentIndex.MoveNext(context);
        }
        else
        {
            var pipeValue = _currentIndex!.MoveNext(context);

            if (pipeValue.IsEmpty) _init = false;

            return pipeValue;
        }
    }

    public void Dispose()
    {
    }
}