namespace LiteDB.Engine;

internal class IndexEqualsEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly IndexDocument _indexDocument;
    private readonly BsonValue _value;

    private bool _init = false;
    private bool _eof = false;

    private PageAddress _prev = PageAddress.Empty; // all nodes from left of first node found
    private PageAddress _next = PageAddress.Empty; // all nodes from right of first node found

    public IndexEqualsEnumerator(
        BsonValue value, 
        IndexDocument indexDocument, 
        Collation collation)
    {
        _value = value;
        _indexDocument = indexDocument;
        _collation = collation;
    }

    public PipeEmit Emit => new(true, false);

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        // in first run, look for index node
        if (!_init)
        {
            _init = true;

            var nodeRef = await indexService.FindAsync(_indexDocument, _value, false, Query.Ascending);

            // if node was not found, end enumerator
            if (nodeRef is null)
            {
                _eof = true;
                return PipeValue.Empty;
            }

            var node = nodeRef.Value.Node;

            if (_indexDocument.Unique)
            {
                _eof = true;
            }
            else
            {
                // get pointer to next/prev at level 0
                _prev = node.Prev[0];
                _next = node.Next[0];
            }

            // current node to return
            return new PipeValue(node.DataBlock);
        }

        // first go forward
        if (!_prev.IsEmpty)
        {
            var nodeRef = await indexService.GetNodeAsync(_prev, false);
            var node = nodeRef.Node;

            var isEqual = _collation.Equals(_value, node.Key);

            if (isEqual)
            {
                _prev = nodeRef.Node.Prev[0];

                return new PipeValue(node.DataBlock);
            }
            else
            {
                _prev = PageAddress.Empty;
            }
        }

        // and than, go backward
        if (!_next.IsEmpty)
        {
            var nodeRef = await indexService.GetNodeAsync(_next, false);
            var node = nodeRef.Node;

            var isEqual = _collation.Equals(_value, node.Key);

            if (isEqual)
            {
                _next = nodeRef.Node.Prev[0];

                return new PipeValue(node.DataBlock);
            }
            else
            {
                _eof = true;
                _next = PageAddress.Empty;
            }
        }

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
