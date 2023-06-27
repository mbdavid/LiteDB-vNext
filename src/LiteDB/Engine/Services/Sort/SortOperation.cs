namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class SortOperation : ISortOperation
{
    // dependency injections
    private readonly ISortService _sortService;

    private readonly BsonExpression _expression;
    private readonly int _order;
    private readonly List<ISortContainer> _containers;

    public SortOperation(
        ISortService sortService,
        BsonExpression expression,
        int order)
    {
        _sortService = sortService;
        _expression = expression;
        _order = order;
    }

    public int[] GetContainersID()
    {
        return _containers.Select(x => x.ContainerID).ToArray();
    }

    public async ValueTask InsertData(IPipeEnumerator enumerator)
    {

    }

    public void Sort()
    {
    }

    public ValueTask<PageAddress> MoveNextAsync()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}
