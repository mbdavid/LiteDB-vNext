namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class SortService : ISortService
{
    private readonly IServicesFactory _factory;
    private readonly IStreamFactory _streamFactory;

    private ConcurrentQueue<int> _availableContainersID = new ();
    private ConcurrentQueue<Stream> _streamPool = new();
    private int _nextContainerID = -1;

    public SortService(IServicesFactory factory, IStreamFactory streamFactory)
    {
        _factory = factory;
        _streamFactory = streamFactory;
    }

    public ISortOperation CreateSort(BsonExpression expression, int order)
    {
        var sorter = _factory.CreateSortOperation(expression, order);

        return sorter;
    }

    public int GetAvailableContainerID()
    {
        if (_availableContainersID.TryDequeue(out var containerID))
        {
            return containerID;
        }

        return Interlocked.Increment(ref _nextContainerID);
    }

    public Stream RentSortStream()
    {
        if (!_streamPool.TryDequeue(out var stream))
        {
            stream = _streamFactory.GetSortStream();
        }

        return stream;
    }

    public void ReleaseSortStream(Stream stream)
    {
        _streamPool.Enqueue(stream);
    }

    public void Dispose()
    {
        foreach(var stream  in _streamPool)
        {
            stream.Dispose();
        }
    }
}
