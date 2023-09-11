namespace LiteDB.Engine;

unsafe internal class IndexEqualsEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly IndexDocument _indexDocument;
    private readonly BsonValue _value;

    private bool _init = false;
    private bool _eof = false;

    private RowID _prev = RowID.Empty; // all nodes from left of first node found
    private RowID _next = RowID.Empty; // all nodes from right of first node found

    public IndexEqualsEnumerator(
        BsonValue value, 
        IndexDocument indexDocument, 
        Collation collation)
    {
        _value = value;
        _indexDocument = indexDocument;
        _collation = collation;
    }

    public PipeEmit Emit => new(true, true, false);

    public unsafe PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        // in first run, look for index node
        if (!_init)
        {
            _init = true;

            var node = indexService.Find(_indexDocument, _value, false, Query.Ascending);

            // if node was not found, end enumerator
            if (node.IsEmpty)
            {
                _eof = true;
                return PipeValue.Empty;
            }

            if (_indexDocument.Unique)
            {
                _eof = true;
            }
            else
            {
                // get pointer to next/prev at level 0
                _prev = node[0]->PrevID;
                _next = node[0]->NextID;
            }

            // current node to return
            return new PipeValue(node.DataBlockID);
        }

        // first go forward
        if (!_prev.IsEmpty)
        {
            var node = indexService.GetNode(_prev);

            //var isEqual = _collation.Equals(_value, node.Key);
            var isEqual = IndexKey.Compare(_value, node.Key, _collation) == 0;

            if (isEqual)
            {
                _prev = node[0]->PrevID;

                return new PipeValue(node.DataBlockID);
            }
            else
            {
                _prev = RowID.Empty;
            }
        }

        // and than, go backward
        if (!_next.IsEmpty)
        {
            var node = indexService.GetNode(_next);

            //var isEqual = _collation.Equals(_value, node.Key);
            var isEqual = IndexKey.Compare(_value, node.Key, _collation) == 0;

            if (isEqual)
            {
                _next = node[0]->PrevID;

                return new PipeValue(node.DataBlockID);
            }
            else
            {
                _eof = true;
                _next = RowID.Empty;
            }
        }

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
