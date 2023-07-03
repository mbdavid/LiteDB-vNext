namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class SortOperation : ISortOperation
{
    // dependency injections
    private readonly ISortService _sortService;
    private readonly Collation _collation;
    private readonly IServicesFactory _factory;

    private Stream? _stream;

    private readonly BsonExpression _expression;
    private readonly int _order;
    private readonly int _containerSize;
    private readonly int _containerSizeLimit;
    private Queue<SortItem>? _sortedItems; // when use less than 1 container

    private byte[]? _containerBuffer = null;
    private BsonValue _current;

    private readonly List<ISortContainer> _containers = new();

    public SortOperation(
        ISortService sortService,
        Collation collation,
        IServicesFactory factory,
        BsonExpression expression,
        int order)
    {
        _sortService = sortService;
        _collation = collation;
        _factory = factory;
        _current = order == 1 ? BsonValue.MinValue : BsonValue.MaxValue;
        _expression = expression;
        _order = order;

        _containerSize = CONTAINER_SORT_SIZE_IN_PAGES * PAGE_SIZE;
        _containerSizeLimit = _containerSize -
            (CONTAINER_SORT_SIZE_IN_PAGES * 2); // used to store int16 on top of each 8k page
    }

    public async ValueTask InsertDataAsync(IPipeEnumerator enumerator, PipeContext context)
    {
        var unsortedItems = new List<SortItem>();
        var remaining = new List<SortItem>();

        var containerBytes = 0;

        while (true)
        {
            var current = await enumerator.MoveNextAsync(context);

            if (current.IsEmpty)
            {
                break;
            }

            var key = _expression.Execute(current.Value, null, _collation);

            var item = new SortItem(current.RowID, key);

            var itemSize = item.GetBytesCount();

            if (containerBytes + itemSize > _containerSizeLimit)
            {
                await this.CreateNewContainerAsync(unsortedItems, remaining);

                containerBytes = 0;
            }

            containerBytes += itemSize;

            unsortedItems.Add(item);
        }

        // if total items are less than 1 container, do not use container (use in-memory quick sort)
        if (_containers.Count == 0)
        {
            // order items
            var query = _order == Query.Ascending ?
                unsortedItems.OrderBy(x => x.Key, _collation) : unsortedItems.OrderByDescending(x => x.Key, _collation);

            _sortedItems = new Queue<SortItem>(query);
        }
        else
        {
            // add last items to new container
            await this.CreateNewContainerAsync(unsortedItems, remaining);

            // initialize cursor readers
            foreach (var container in _containers)
            {
                await container.MoveNextAsync();
            }
        }
    }

    private async ValueTask CreateNewContainerAsync(List<SortItem> unsortedItems, List<SortItem> remaining)
    {
        // rent container byffer array
        _containerBuffer ??= ArrayPool<byte>.Shared.Rent(_containerSize);

        // rent a exclusive stream for this sort operation
        _stream ??= _sortService.RentSortStream();

        // get a new containerID 
        var containerID = _sortService.GetAvailableContainerID();

        // create new sort container
        var container = _factory.CreateSortContainer(containerID, _order, _stream);

        // sort all items into 8k pages and returns "remaining" items if not fit on container size
        container.Sort(unsortedItems, _containerBuffer, remaining);

        // clear container items and add any remaining item
        unsortedItems.Clear();
        unsortedItems.AddRange(remaining);
        remaining.Clear();

        // position stream in container disk position
        _stream.Position = containerID * _containerSize;

        await _stream.WriteAsync(_containerBuffer);

        _containers.Add(container);
    }

    public async ValueTask<PageAddress> MoveNextAsync()
    {
        // when use in-memory sort
        if (_sortedItems is not null)
        {
            if (_sortedItems.Count == 0) return PageAddress.Empty;

            var item = _sortedItems.Dequeue();

            return item.RowID;
        }
        // when use multiple containers
        else
        {
            ISortContainer? next = null;

            // get lower/hightest value from all containers
            foreach (var container in _containers)
            {
                var diff = container.Current.Key.CompareTo(_current, _collation);

                if (diff == (_order * -1))
                {
                    next = container;
                }
            }

            _current = next!.Current.Key;

            await next.MoveNextAsync();

            // if there is no more items on container, remove from container list (dispose before)
            if (next.Remaining == 0)
            {
                _containers.Remove(next);

                next.Dispose();
            }

            return next.Current.RowID;
        }
    }

    public void Dispose()
    {
        // release rented stream
        if (_stream is not null)
        {
            _sortService.ReleaseSortStream(_stream);
        }

        // dispose all containers (release PageBuffers)
        foreach(var container in _containers)
        {
            container.Dispose();
        }

        // release container buffer (byte[])
        if(_containerBuffer is not null)
        {
            ArrayPool<byte>.Shared.Return(_containerBuffer);
        }
    }
}
