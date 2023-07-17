namespace LiteDB.Engine;

internal class IndexScanEnumerator : IPipeEnumerator
{

    private readonly IndexDocument _indexDocument;
    private readonly Func<BsonValue, bool> _func;
    private readonly int _order;

    private bool _init = false;
    private bool _eof = false;

    private PageAddress _next = PageAddress.Empty; // all nodes from right of first node found

    public IndexScanEnumerator(
        IndexDocument indexDocument,
        Func<BsonValue, bool> func,
        int order)
    {
        _indexDocument = indexDocument;
        _func = func;
        _order = order;
    }

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        // in first run, gets head node
        if (!_init)
        {
            _init = true;

            var start = _order == Query.Ascending ? _indexDocument.Head : _indexDocument.Tail;

            var nodeRef = await indexService.GetNodeAsync(start, false);

            // get pointer to next at level 0
            _next = nodeRef.Node.GetNextPrev(0, _order);

            if(_func(nodeRef.Node.Key))
            {
                return new PipeValue(nodeRef.Node.DataBlock);
            }
        }

        // go forward
        if (!_next.IsEmpty)
        {
            do
            {
                var nodeRef = await indexService.GetNodeAsync(_next, false);
                var node = nodeRef.Node;

                _next = nodeRef.Node.GetNextPrev(0, _order);

                if (_func(nodeRef.Node.Key))
                {
                    return new PipeValue(nodeRef.Node.DataBlock);
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
