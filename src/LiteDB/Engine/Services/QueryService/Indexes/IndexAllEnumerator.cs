namespace LiteDB.Engine;

internal class IndexAllEnumerator : IPipeEnumerator
{
    private readonly IndexDocument _indexDocument;
    private readonly int _order;

    private bool _init = false;
    private bool _eof = false;

    private RowID _next = RowID.Empty; // all nodes from right of first node found

    public IndexAllEnumerator(
        IndexDocument indexDocument, 
        int order)
    {
        _indexDocument = indexDocument;
        _order = order;
    }

    public PipeEmit Emit => new(true, true, false);

    public unsafe PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        var head = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;
        var tail = _order == Query.Ascending ? _indexDocument.TailIndexNodeID : _indexDocument.HeadIndexNodeID;

        // in first run, gets head node
        if (_init == false)
        {
            _init = true;

            var first = indexService.GetNode(head);

            // get pointer to first element 
            _next = first[0]->GetNext(_order);

            // check if not empty
            if (_next == tail)
            {
                _eof = true;
                return PipeValue.Empty;
            }
        }

        // go forward
        var node = indexService.GetNode(_next);

        _next = node[0]->GetNext(_order);

        if (_next == tail) _eof = true;

        return new PipeValue(node.DataBlockID);
    }

    public void Dispose()
    {
    }
}
