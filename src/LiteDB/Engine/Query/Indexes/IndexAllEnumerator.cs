using System;

namespace LiteDB.Engine;

internal class IndexAllEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly IndexDocument _indexDocument;

    private readonly int _order;

    private bool _init = false;
    private bool _eof = false;


    private PageAddress _prev = PageAddress.Empty; // all nodes from left of first node found
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

            // get pointer to next
            _next = nodeRef.Node.GetNextPrev(0, _order);

            return new PipeValue(nodeRef.Node.RowID);
        }
        // go forward
        if (!_next.IsEmpty)
        {
            var nodeRef = await indexService.GetNodeAsync(_next, false);

            _next = nodeRef.Node.GetNextPrev(0, _order);

            return new PipeValue(nodeRef.Node.RowID);

        }

        _eof = true;

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
