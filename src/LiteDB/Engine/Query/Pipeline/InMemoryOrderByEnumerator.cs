namespace LiteDB.Engine;

internal class InMemoryOrderByEnumerator : IPipeEnumerator
{
    private readonly BsonExpression _expr;
    private readonly int _order;
    private readonly Collation _collation;
    private readonly IPipeEnumerator _enumerator;
    private readonly Queue<SortItemDocument> _sorted = new();

    private bool _init = false;
    private bool _eof = false;

    public InMemoryOrderByEnumerator(BsonExpression expr, int order, Collation collation, IPipeEnumerator enumerator)
    {
        _expr = expr;
        _order = order;
        _collation = collation;
        _enumerator = enumerator;
    }

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (!_init)
        {
            var list = new List<SortItemDocument>();

            while (true)
            {
                var item = await _enumerator.MoveNextAsync(context);

                if (item.IsEmpty) break;

                // get sort key 
                var key = _expr.Execute(item.Value, null, _collation);

                list.Add(new (item.RowID, key, item.Value!));
            }

            // sort list in a new enumerable
            var query = _order == Query.Ascending ?
                list.OrderBy(x => x.Key, _collation) : list.OrderByDescending(x => x.Key, _collation);

            // add items in a sorted queue
            foreach (var item in query)
            {
                _sorted.Enqueue(item);
            }
        }

        if (_sorted.Count > 0)
        {
            var item = _sorted.Dequeue();

            _eof = _sorted.Count > 0;

            return new PipeValue(item.RowID, item.Document);
        }

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
