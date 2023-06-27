namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class SortService : ISortService
{
    private readonly IServicesFactory _factory;

    private ConcurrentQueue<int> _avaiableContainersID = new ();
    private int _nextContainerID = -1;

    public ISortOperation CreateSort(BsonExpression expression, int order)
    {
        var sorter = _factory.CreateSortOperation(expression, order);

        return sorter;
    }

    public int GetAvaiableContainerID()
    {
        if (_avaiableContainersID.TryDequeue(out var containerID))
        {
            return containerID;
        }

        return Interlocked.Increment(ref _nextContainerID);
    }

    public void ReleaseSort(ISortOperation sorter)
    {
        foreach (var containerID in sorter.GetContainersID())
        {
            _avaiableContainersID.Enqueue(containerID);
        }

        sorter.Dispose();
    }

    public void Dispose()
    {
    }
}
