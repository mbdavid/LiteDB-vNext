//namespace LiteDB.Engine;

//internal class IndexRangeEnumerator : IPipeEnumerator
//{
//    private readonly Collation _collation;

//    private readonly IndexDocument _indexDocument;
//    private readonly BsonValue _start;
//    private readonly BsonValue _end;

//    private readonly bool _startEquals;
//    private readonly bool _endEquals;
//    private readonly int _order;
//    private bool _init = false;
//    private bool _eof = false;

//    private PageAddress _prev = PageAddress.Empty; // all nodes from left of first node found
//    private PageAddress _next = PageAddress.Empty; // all nodes from right of first node found

//    public IndexRangeEnumerator(
//        BsonValue start, 
//        BsonValue end, 
//        bool startEquals, 
//        bool endEquals,
//        int order,
//        IndexDocument indexDocument, 
//        Collation collation)
//    {
//        _start = start;
//        _end = end;
//        _startEquals = startEquals;
//        _endEquals = endEquals;
//        _order = order;
//        _indexDocument = indexDocument;
//        _collation = collation;
//    }

//    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
//    {
//        if (_eof) return PipeValue.Empty;

//        var indexService = context.IndexService;
//        var dataBlock = PageAddress.Empty;

//        // in first run, look for index node
//        if (!_init)
//        {
//            // if order are desc, swap start/end values
//            var start = _order == Query.Ascending ? _start : _end;
//            var end = _order == Query.Ascending ? _end : _start;

//            var startEquals = _order == Query.Ascending ? _startEquals : _endEquals;
//            var endEquals = _order == Query.Ascending ? _endEquals : _startEquals;

//            // find first indexNode (or get from head/tail if Min/Max value)
//            var firstRef =
//                start.Type == BsonType.MinValue ? await indexService.GetNodeAsync(_indexDocument.Head, false) :
//                start.Type == BsonType.MaxValue ? await indexService.GetNodeAsync(_indexDocument.Tail, false) :
//                await indexService.FindAsync(_indexDocument, start, true, _order);

//            // get pointer to next/prev at level 0
//            _prev = node.Prev[0];
//            _next = node.Next[0];


//            var node = first;



//            var nodeRef = await indexService.FindAsync(_indexDocument, _value, false, Query.Ascending);

//            // if node was not found, end enumerator
//            if (nodeRef is null)
//            {
//                _init = _eof = true;
//                return PipeValue.Empty;
//            }

//            var node = nodeRef.Value.Node;

//            // current node to return
//            dataBlock = node.DataBlock;

//            if (_indexDocument.Unique)
//            {
//                _eof = true;
//            }
//            else
//            {
//                // get pointer to next/prev at level 0
//                _prev = node.Prev[0];
//                _next = node.Next[0];
//            }

//            _init = true;
//        }
//        // first go forward
//        else if (!_next.IsEmpty)
//        {
//            var nodeRef = await indexService.GetNodeAsync(_next, false);
//            var node = nodeRef.Node;

//            var isEqual = _collation.Equals(_value, node.Key);

//            if (isEqual)
//            {
//                dataBlock = node.DataBlock;

//                _next = nodeRef.Node.Next[0];
//            }
//            else
//            {
//                _eof = true;
//            }
//        }
//        // and than, go backward
//        else if (!_prev.IsEmpty)
//        {
//            var nodeRef = await indexService.GetNodeAsync(_prev, false);
//            var node = nodeRef.Node;

//            var isEqual = _collation.Equals(_value, node.Key);

//            if (isEqual)
//            {
//                dataBlock = node.DataBlock;

//                _next = nodeRef.Node.Prev[0];
//            }
//            else
//            {
//                _eof = true;
//            }
//        }

//        // return current dataBlock rowID
//        return new PipeValue(dataBlock);
//    }

//    public void Dispose()
//    {
//    }
//}
