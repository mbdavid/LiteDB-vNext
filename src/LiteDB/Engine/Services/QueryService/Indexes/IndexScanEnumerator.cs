namespace LiteDB.Engine;

internal class IndexScanEnumerator : IPipeEnumerator
{
    private readonly IndexDocument _indexDocument;
    private readonly Func<BsonValue, bool> _func;
    private readonly int _order;

    private bool _init = false;
    private bool _eof = false;

    private RowID _next = RowID.Empty; // all nodes from right of first node found

    public IndexScanEnumerator(
        IndexDocument indexDocument,
        Func<BsonValue, bool> func,
        int order)
    {
        _indexDocument = indexDocument;
        _func = func;
        _order = order;
    }

    public PipeEmit Emit => new(true, true, false);

    public unsafe PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        // in first run, gets head node
        if (!_init)
        {
            _init = true;

            var start = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;

            var node = indexService.GetNode(start);

            // get pointer to next at level 0
            _next = node[0]->GetNextPrev(_order);

            // get key as BsonValue to run computed function
            var key = IndexKey.ToBsonValue(node.Key);

            if(_func(key))
            {
                return new PipeValue(node.DataBlockID);
            }
        }

        // go forward
        if (!_next.IsEmpty)
        {
            do
            {
                var node = indexService.GetNode(_next);

                _next = node[0]->GetNextPrev(_order);

                // get key as BsonValue to run computed function
                var key = IndexKey.ToBsonValue(node.Key);

                if (_func(key))
                {
                    return new PipeValue(node.DataBlockID);
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
