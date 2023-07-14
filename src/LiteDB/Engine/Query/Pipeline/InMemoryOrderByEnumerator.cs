namespace LiteDB.Engine;

internal class InMemoryOrderByEnumerator : IPipeEnumerator
{
    private readonly BsonExpression _expr;
    private readonly int _order;
    private readonly Collation _collation;
    private readonly IPipeEnumerator _enumerator;
    private Queue<SortItemDocument>? _sortedItems;

    public InMemoryOrderByEnumerator(BsonExpression expr, int order, Collation collation, IPipeEnumerator enumerator)
    {
        _expr = expr;
        _order = order;
        _collation = collation;
        _enumerator = enumerator;
    }

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_sortedItems is null)
        {
            var list = new List<SortItemDocument>();

            while (true)
            {
                var item = await _enumerator.MoveNextAsync(context);

                if (item.IsEmpty) break;

                // get sort key 
                var key = _expr.Execute(item.Value, context.QueryParameters, _collation);

                list.Add(new (item.RowID, key, item.Value!));
            }

            // sort list in a new enumerable
            var query = _order == Query.Ascending ?
                list.OrderBy(x => x.Key, _collation) : list.OrderByDescending(x => x.Key, _collation);

            _sortedItems = new Queue<SortItemDocument>(query);
        }

        if (_sortedItems.Count > 0)
        {
            var item = _sortedItems.Dequeue();

            return new PipeValue(item.RowID, item.Document);
        }

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
