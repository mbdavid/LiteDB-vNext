namespace LiteDB.Engine;

internal class IndexScanEnumerator : IPipeEnumerator
{

    private readonly __IndexDocument _indexDocument;
    private readonly Func<BsonValue, bool> _func;
    private readonly int _order;

    private bool _init = false;
    private bool _eof = false;

    private PageAddress _next = PageAddress.Empty; // all nodes from right of first node found

    public IndexScanEnumerator(
        __IndexDocument indexDocument,
        Func<BsonValue, bool> func,
        int order)
    {
        _indexDocument = indexDocument;
        _func = func;
        _order = order;
    }

    public PipeEmit Emit => new(true, true, false);

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        // in first run, gets head node
        if (!_init)
        {
            _init = true;

            var start = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;

            var nodeRef = await indexService.GetNodeAsync(start);

            // get pointer to next at level 0
            _next = nodeRef.Node.GetNextPrev(0, _order);

            if(_func(nodeRef.Node.Key))
            {
                return new PipeValue(nodeRef.Node.IndexNodeID, nodeRef.Node.DataBlockID);
            }
        }

        // go forward
        if (!_next.IsEmpty)
        {
            do
            {
                var nodeRef = await indexService.GetNodeAsync(_next);
                var node = nodeRef.Node;

                _next = nodeRef.Node.GetNextPrev(0, _order);

                if (_func(nodeRef.Node.Key))
                {
                    return new PipeValue(nodeRef.Node.IndexNodeID, nodeRef.Node.DataBlockID);
                }

            } while (!_next.IsEmpty);
        }

        _eof = true;

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
