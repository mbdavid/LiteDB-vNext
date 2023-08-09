namespace LiteDB.Engine;

internal class IndexRangeEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly IndexDocument _indexDocument;
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
        IndexDocument indexDocument,
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

    public PipeEmit Emit => new(true, false);

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        // in first run, look for index node
        if (!_init)
        {
            _init = true;

            // find first indexNode (or get from head/tail if Min/Max value)
            var firstRef =
                _start.IsMinValue ? await indexService.GetNodeAsync(_indexDocument.Head, false) :
                _start.IsMaxValue ? await indexService.GetNodeAsync(_indexDocument.Tail, false) :
                await indexService.FindAsync(_indexDocument, _start, true, _order);

            // get pointer to next/prev at level 0
            _prev = firstRef.Value.Node.Prev[0];
            _next = firstRef.Value.Node.Next[0];

            if (_startEquals && firstRef is not null)
            {
                var node = firstRef.Value.Node;

                if (!node.Key.IsMinValue && !node.Key.IsMaxValue)
                { 
                    return new PipeValue(firstRef.Value.Node.DataBlock);
                }
            }
        }

        // first go forward
        if (!_prev.IsEmpty)
        {
            var nodeRef = await indexService.GetNodeAsync(_prev, false);
            var node = nodeRef.Node;

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
                    _prev = nodeRef.Node.Prev[0];

                    return new PipeValue(node.DataBlock);
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
            var nodeRef = await indexService.GetNodeAsync(_next, false);
            var node = nodeRef.Node;

            // check for Min/Max bson values index node key
            if (node.Key.IsMaxValue || node.Key.IsMinValue)
            {
                _eof = true;

                return PipeValue.Empty;
            }

            var diff = _collation.Compare(_end, node.Key);

            if (diff == (_order * -1 /* -1 */) || (diff == 0 && _endEquals))
            {
                _next = nodeRef.Node.Next[0];

                return new PipeValue(node.DataBlock);
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
