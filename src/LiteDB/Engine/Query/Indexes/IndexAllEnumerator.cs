namespace LiteDB.Engine;

internal class IndexAllEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;
    private readonly IndexDocument _indexDocument;
    private readonly int _order;

    private bool _init = false;
    private bool _eof = false;

    private PageAddress _next = PageAddress.Empty; // all nodes from right of first node found

    public IndexAllEnumerator(
        IndexDocument indexDocument, 
        Collation collation,
        int order)
    {
        _indexDocument = indexDocument;
        _collation = collation;
        _order = order;
    }

    public PipeEmit Emit => new(true, false);

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;
        var start = _order == Query.Ascending ? _indexDocument.Head : _indexDocument.Tail;
        var end = _order == Query.Ascending ? _indexDocument.Tail : _indexDocument.Head;

        // in first run, gets head node
        if (!_init)
        {
            _init = true;

            var (node, _) = await indexService.GetNodeAsync(start);

            // get pointer to first element 
            _next = node.GetNextPrev(0, _order);

            // check if not empty
            if (_next == end)
            {
                _eof = true;

                return PipeValue.Empty;
            }
        }

        // go forward
        if (_next != end || _next.IsEmpty)
        {
            var (node, _) = await indexService.GetNodeAsync(_next);

            _next = node.GetNextPrev(0, _order);

            return new PipeValue(node.DataBlock);
        }

        _eof = true;

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
