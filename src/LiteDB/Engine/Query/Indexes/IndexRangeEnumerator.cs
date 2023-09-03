namespace LiteDB.Engine;

internal class IndexRangeEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly __IndexDocument _indexDocument;
    private readonly BsonValue _start;
    private readonly BsonValue _end;

    private readonly bool _startEquals;
    private readonly bool _endEquals;
    private readonly int _order;
    private bool _init = false;
    private bool _eof = false;

    private PageAddress _prev = PageAddress.Empty; // all nodes from left of first node found
    private PageAddress _next = PageAddress.Empty; // all nodes from right of first node found

    public IndexRangeEnumerator(
        BsonValue start,
        BsonValue end,
        bool startEquals,
        bool endEquals,
        int order,
        __IndexDocument indexDocument,
        Collation collation)
    {
        // if order are desc, swap start/end values
        _start = order == Query.Ascending ? start : end;
        _end = order == Query.Ascending ? end : start;
        _startEquals = order == Query.Ascending ? startEquals : endEquals;
        _endEquals = order == Query.Ascending ? endEquals : startEquals;
        _order = order;
        _indexDocument = indexDocument;
        _collation = collation;
    }

    public PipeEmit Emit => new(true, true, false);

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        // in first run, look for index node
        if (!_init)
        {
            _init = true;

            // find first indexNode (or get from head/tail if Min/Max value)
            var (first, _) =
                _start.IsMinValue ? await indexService.GetNodeAsync(_indexDocument.HeadIndexNodeID) :
                _start.IsMaxValue ? await indexService.GetNodeAsync(_indexDocument.TailIndexNodeID) :
                await indexService.FindAsync(_indexDocument, _start, true, _order);

            // get pointer to next/prev at level 0
            _prev = first.Prev[0];
            _next = first.Next[0];

            if (_startEquals && !first.IsEmpty)
            {
                if (!first.Key.IsMinValue && !first.Key.IsMaxValue)
                { 
                    return new PipeValue(first.IndexNodeID, first.DataBlockID);
                }
            }
        }

        // first go forward
        if (!_prev.IsEmpty)
        {
            var (node, _) = await indexService.GetNodeAsync(_prev);

            // check for Min/Max bson values index node key
            if (node.Key.IsMaxValue || node.Key.IsMinValue)
            {
                _prev = PageAddress.Empty;
            }
            else
            {
                var diff = _collation.Compare(_start, node.Key);

                if (diff == (_order /* 1 */) || (diff == 0 && _startEquals))
                {
                    _prev = node.Prev[0];

                    return new PipeValue(node.IndexNodeID, node.DataBlockID);
                }
                else
                {
                    _prev = PageAddress.Empty;
                }
            }
        }

        // and than, go backward
        if (!_next.IsEmpty)
        {
            var (node, _) = await indexService.GetNodeAsync(_next);

            // check for Min/Max bson values index node key
            if (node.Key.IsMaxValue || node.Key.IsMinValue)
            {
                _eof = true;

                return PipeValue.Empty;
            }

            var diff = _collation.Compare(_end, node.Key);

            if (diff == (_order * -1 /* -1 */) || (diff == 0 && _endEquals))
            {
                _next = node.Next[0];

                return new PipeValue(node.IndexNodeID, node.DataBlockID);
            }
            else
            {
                _eof = true;
                _next = PageAddress.Empty;
            }
        }

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
