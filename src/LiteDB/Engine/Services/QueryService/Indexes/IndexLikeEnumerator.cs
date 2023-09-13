using System;

namespace LiteDB.Engine;

unsafe internal class IndexLikeEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly IndexDocument _indexDocument;
    private readonly BsonValue _value;
    private readonly string _startsWith;
    private readonly bool _hasMore;
    private readonly int _order;

    private bool _init = false;
    private bool _eof = false;

    private RowID _prev = RowID.Empty; // all nodes from left of first node found
    private RowID _next = RowID.Empty; // all nodes from right of first node found

    public IndexLikeEnumerator(
        BsonValue value, 
        IndexDocument indexDocument, 
        Collation collation,
        int order)
    {
        _startsWith = value.AsString.SqlLikeStartsWith(out _hasMore);
        _value = value;
        _indexDocument = indexDocument;
        _collation = collation;
        _order = order;
    }

    public PipeEmit Emit => new(true, true, false);

    public PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        return _startsWith.Length > 0 ?  this.ExecuteLike(context) : this.ExecuteFullScan(context);
    }

    private unsafe PipeValue ExecuteLike(PipeContext context)
    {
        var indexService = context.IndexService;

        if (!_init)
        {
            _init = true;

            var node = indexService.Find(_indexDocument, _startsWith, true, Query.Ascending);

            if (node.IsEmpty)
            {
                _eof = true;
                return PipeValue.Empty;
            };

            // get pointer to next/prev
            _prev = node[0]->PrevID;
            _next = node[0]->NextID;
        }

        if (!_next.IsEmpty || !_prev.IsEmpty)
        {
            var node = !_next.IsEmpty ?
                indexService.GetNode(_next) :
                indexService.GetNode(_prev);

            if (node.Key->Type == BsonType.String)
            {
                var key = IndexKey.ToBsonValue(node.Key).AsString;

                if (key.StartsWith(_startsWith, StringComparison.OrdinalIgnoreCase))
                {
                    if (!_hasMore || (_hasMore && key.SqlLike(_value, _collation)))
                    {
                        return new PipeValue(node.DataBlockID);
                    }
                }
            }

        }
        return PipeValue.Empty;
    }

    private unsafe PipeValue ExecuteFullScan(PipeContext context)
    {
        var indexService = context.IndexService;

        if (_eof) return PipeValue.Empty;

        // in first run, gets head node
        if (!_init)
        {
            _init = true;

            var start = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;

            var node = indexService.GetNode(start);

            // get pointer to next at level 0
            _next = node[0]->GetNextPrev(_order);

            if (node.Key->Type == BsonType.String)
            {
                var key = IndexKey.ToBsonValue(node.Key).AsString;

                if (key.SqlLike(_value, _collation))
                {
                    return new PipeValue(node.DataBlockID);
                }
            }
        }
        // go forward
        if (!_next.IsEmpty)
        {
            do
            {
                var node = indexService.GetNode(_next);

                if (node.Key->Type == BsonType.String)
                {
                    var key = IndexKey.ToBsonValue(node.Key).AsString;

                    if (key.SqlLike(_value, _collation))
                    {
                        return new PipeValue(node.DataBlockID);
                    }
                }

                _next = node[0]->GetNextPrev(_order);

            } while (!_next.IsEmpty);
        }

        _eof = true;

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
