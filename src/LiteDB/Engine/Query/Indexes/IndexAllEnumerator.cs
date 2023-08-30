namespace LiteDB.Engine;

internal class IndexAllEnumerator : IPipeEnumerator
{
    private readonly IndexDocument _indexDocument;
    private readonly int _order;

    private bool _init = false;
    private bool _eof = false;

    private PageAddress _next = PageAddress.Empty; // all nodes from right of first node found

    public IndexAllEnumerator(
        IndexDocument indexDocument, 
        int order)
    {
        _indexDocument = indexDocument;
        _order = order;
    }

    public PipeEmit Emit => new(true, true, false);

    public PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;
        var start = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;
        var end = _order == Query.Ascending ? _indexDocument.TailIndexNodeID : _indexDocument.HeadIndexNodeID;

        // in first run, gets head node
        if (!_init)
        {
            _init = true;

            var (node, _) = indexService.GetNode(start);

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
            var (node, _) = indexService.GetNode(_next);

            _next = node.GetNextPrev(0, _order);

            return new PipeValue(node.IndexNodeID, node.DataBlockID);
        }

        _eof = true;

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
