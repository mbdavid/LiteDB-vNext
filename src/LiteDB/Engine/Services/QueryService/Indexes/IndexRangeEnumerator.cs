namespace LiteDB.Engine;

unsafe internal class IndexRangeEnumerator : IPipeEnumerator
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

    private RowID _prev = RowID.Empty; // all nodes from left of first node found
    private RowID _next = RowID.Empty; // all nodes from right of first node found

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

    public PipeEmit Emit => new(true, true, false);

    public unsafe PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        // in first run, look for index node
        if (!_init)
        {
            _init = true;

            // find first indexNode (or get from head/tail if Min/Max value)
            var first =
                _start.IsMinValue ? indexService.GetNode(_indexDocument.HeadIndexNodeID) :
                _start.IsMaxValue ? indexService.GetNode(_indexDocument.TailIndexNodeID) :
                indexService.Find(_indexDocument, _start, true, _order);

            // get pointer to next/prev at level 0
            _prev = first[0]->PrevID;
            _next = first[0]->NextID;

            if (_startEquals && !first.IsEmpty)
            {
                if (!first.Key->IsMinValue && !first.Key->IsMaxValue)
                { 
                    return new PipeValue(first.IndexNodeID, first.DataBlockID);
                }
            }
        }

        // first go forward
        if (!_prev.IsEmpty)
        {
            var node = indexService.GetNode(_prev);

            // check for Min/Max bson values index node key
            if (node.Key->IsMaxValue || node.Key->IsMinValue)
            {
                _prev = RowID.Empty;
            }
            else
            {
                //***var diff = _collation.Compare(_start, node.Key);
                var diff = IndexKey.Compare(_start, node.Key, _collation);

                if (diff == (_order /* 1 */) || (diff == 0 && _startEquals))
                {
                    _prev = node[0]->PrevID;

                    return new PipeValue(node.IndexNodeID, node.DataBlockID);
                }
                else
                {
                    _prev = RowID.Empty;
                }
            }
        }

        // and than, go backward
        if (!_next.IsEmpty)
        {
            var node = indexService.GetNode(_next);

            // check for Min/Max bson values index node key
            if (node.Key->IsMaxValue || node.Key->IsMinValue)
            {
                _eof = true;

                return PipeValue.Empty;
            }

            //***var diff = _collation.Compare(_end, node.Key);
            var diff = IndexKey.Compare(_end, node.Key, _collation);

            if (diff == (_order * -1 /* -1 */) || (diff == 0 && _endEquals))
            {
                _next = node[0]->NextID;

                return new PipeValue(node.IndexNodeID, node.DataBlockID);
            }
            else
            {
                _eof = true;
                _next = RowID.Empty;
            }
        }

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
