namespace LiteDB.Engine;

internal class MemoryCachePage
{
    private int _sharedCounter = 1;

    //public long Position { get; set; } = long.MaxValue;
    public int ShareCounter { get; private set; } = 1;
    public long Timestamp { get; private set; } = DateTime.UtcNow.Ticks;
    public Memory<byte> Buffer => _owner.Memory;
    public BasePage Page { get; }

    private readonly IMemoryOwner<byte> _owner;

    public MemoryCachePage()
    {
        _owner = MemoryPool<byte>.Shared.Rent(PAGE_SIZE);
    }

    public void Rent()
    {
        Interlocked.Increment(ref _sharedCounter);
    }

    public void Return()
    {
        Interlocked.Decrement(ref _sharedCounter);
    }

    public void Dispose()
    {
        _owner.Dispose();
    }
}
