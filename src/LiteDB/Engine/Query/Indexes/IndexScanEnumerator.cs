namespace LiteDB.Engine;

internal class IndexScanEnumerator : IPipeEnumerator
{

    private readonly IndexDocument _indexDocument;
    private readonly Func<BsonValue, bool> _func;

    private bool _init = false;
    private bool _eof = false;

    private PageAddress _next = PageAddress.Empty; // all nodes from right of first node found

    public IndexScanEnumerator(
        IndexDocument indexDocument,
        Func<BsonValue, bool> func)
    {
        _indexDocument = indexDocument;
        _func = func;
    }

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        // in first run, gets head node
        if (!_init)
        {
            _init = true;

            var nodeRef = await indexService.GetNodeAsync(_indexDocument.Head, false);

            // get pointer to next at level 0
            //nodeRef.Node.GetNextPrev(0, Query.Ascending);
            _next = nodeRef.Node.Next[0];

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

                _next = nodeRef.Node.Next[0];

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
