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

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;
        var dataBlock = PageAddress.Empty;

        // in first run, look for index node
        if (!_init)
        {
            var nodeRef = await indexService.FindAsync(_indexDocument, _value, false, Query.Ascending);

            // if node was not found, end enumerator
            if (nodeRef is null)
            {
                _init = _eof = true;
                return PipeValue.Empty;
            }

            var node = nodeRef.Value.Node;

            // current node to return
            dataBlock = node.DataBlock;

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

            _init = true;
        }
        // first go forward
        else if (!_next.IsEmpty)
        {
            var nodeRef = await indexService.GetNodeAsync(_next, false);
            var node = nodeRef.Node;

            var isEqual = _collation.Equals(_value, node.Key);

            if (isEqual)
            {
                dataBlock = node.DataBlock;

                _next = nodeRef.Node.Next[0];
            }
            else
            {
                _eof = true;
            }
        }
        // and than, go backward
        else if (!_prev.IsEmpty)
        {
            var nodeRef = await indexService.GetNodeAsync(_prev, false);
            var node = nodeRef.Node;

            var isEqual = _collation.Equals(_value, node.Key);

            if (isEqual)
            {
                dataBlock = node.DataBlock;

                _next = nodeRef.Node.Prev[0];
            }
            else
            {
                _eof = true;
            }
        }

        // return current dataBlock rowID
        return new PipeValue(dataBlock);
    }

    public void Dispose()
    {
    }
}
