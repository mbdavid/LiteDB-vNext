namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class SortContainer : ISortContainer
{
    // dependency injections
    private readonly IBufferFactory _bufferFactory;
    private readonly Collation _collation;

    private readonly int _containerID;
    private PageBuffer _buffer;
    private BsonValue _current;
    private int _positionID;
    private int _readPosition;

    public SortContainer(int containerID, int containerSize)
    {
        _containerID = containerID;
    }

    public int ContainerID => _containerID;

    public void Sort(IList<SortItem> items, int order, Span<byte> buffer)
    {
        var query = order == Query.Ascending ?
            items.OrderBy(x => x.Key, _collation) : items.OrderByDescending(x => x.Key, _collation);

        var offset = 0;

        foreach(var item in query)
        {
            buffer[offset..].WriteBsonValue(item.Key, out var len);

            offset += len;

            buffer[offset..].WritePageAddress(item.RowID);

        }


        _readBuffer = _bufferFactory.AllocateNewPage(false);
    }

    public void Dispose()
    {
        if (_readBuffer is not null)
        {
            _bufferFactory.DeallocatePage(_readBuffer);
        }
    }
}
