using System;
using System.Net.Http.Headers;
using System.Xml.Linq;

namespace LiteDB.Engine;

internal class IndexLikeEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly IndexDocument _indexDocument;
    private readonly BsonValue _value;
    private readonly string _startsWith;
    private readonly bool _hasMore;

    private bool _init = false;
    private bool _eof = false;

    private PageAddress _prev = PageAddress.Empty; // all nodes from left of first node found
    private PageAddress _next = PageAddress.Empty; // all nodes from right of first node found

    public IndexLikeEnumerator(
        BsonValue value, 
        IndexDocument indexDocument, 
        Collation collation)
    {
        _startsWith = value.AsString.SqlLikeStartsWith(out _hasMore);
        _value = value;
        _indexDocument = indexDocument;
        _collation = collation;
    }

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        return _startsWith.Length > 0 ? await executeLike(context) : await executeFullScan(context);
    }

    private async ValueTask<PipeValue> executeLike(PipeContext context)
    {
        var indexService = context.IndexService;

        if(!_init)
        {
            _init = true;
            var node = await indexService.FindAsync(_indexDocument, _startsWith, true, Query.Ascending);
            if (node == null) return PipeValue.Empty;

            // get pointer to next/prev
            _prev = node.Value.Node.Prev[0];
            _next = node.Value.Node.Next[0];
        }
        if(!_next.IsEmpty || !_prev.IsEmpty)
        {
            IndexNode node;
            if (!_next.IsEmpty)
            {
                var nodeRef = await indexService.GetNodeAsync(_next, false);
                node = nodeRef.Node;
            }
            else
            {
                var nodeRef = await indexService.GetNodeAsync(_prev, false);
                node = nodeRef.Node;
            }
            

            if(node.Key.AsString.StartsWith(_startsWith, StringComparison.OrdinalIgnoreCase))
            {
                if(!_hasMore || (_hasMore && node.Key.AsString.SqlLike(_value, _collation)))
                {
                    return new PipeValue(node.DataBlock);
                }
            }
        }
        return PipeValue.Empty;
    }

    private async ValueTask<PipeValue> executeFullScan(PipeContext context)
    {
        var indexService = context.IndexService;
        if (_eof) return PipeValue.Empty;

        // in first run, gets head node
        if (!_init)
        {
            _init = true;
            var nodeRef = await indexService.GetNodeAsync(_indexDocument.Head, false);
            var node = nodeRef.Node;

            // get pointer to next
            _next = nodeRef.Node.Next[0];

            if (node.Key.AsString.SqlLike(_value, _collation))
            {
                return new PipeValue(node.DataBlock);
            }
        }
        // go forward
        if (!_next.IsEmpty)
        {
            do
            {
                var nodeRef = await indexService.GetNodeAsync(_next, false);
                var node = nodeRef.Node;
                if (node.Key.AsString.SqlLike(_value, _collation))
                {
                    return new PipeValue(node.DataBlock);
                }
                _next = nodeRef.Node.Next[0];
            } while (!_next.IsEmpty);
        }
        _eof = true;
        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
