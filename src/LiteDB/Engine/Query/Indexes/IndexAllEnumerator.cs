using System;

namespace LiteDB.Engine;

internal class IndexAllEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly IndexDocument _indexDocument;

    private bool _init = false;
    private bool _eof = false;

    private PageAddress _prev = PageAddress.Empty; // all nodes from left of first node found
    private PageAddress _next = PageAddress.Empty; // all nodes from right of first node found

    public IndexAllEnumerator(
        IndexDocument indexDocument, 
        Collation collation)
    {
        _indexDocument = indexDocument;
        _collation = collation;
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
            _next = nodeRef.Node.Next[0];

            return new PipeValue(nodeRef.Node.RowID);
        }
        // go forward
        if (!_next.IsEmpty)
        {
            var nodeRef = await indexService.GetNodeAsync(_next, false);
            _next = nodeRef.Node.Next[0];
            return new PipeValue(nodeRef.Node.RowID);

        }

        _eof = true;

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
