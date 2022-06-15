namespace LiteDB.Engine;

/// <summary>
/// Each memory cache page represent a shared buffer with PAGE_SIZE. 
/// Implements IDisposable when page
/// </summary>
internal class MemoryCachePage : IDisposable
{
    private int _sharedCounter = 1;

    public int ShareCounter { get; private set; } = 1;
    public long Timestamp { get; private set; } = DateTime.UtcNow.Ticks;
    public readonly Memory<byte> Buffer;
    public BasePage Page { get; set; }

    private readonly byte[] _bufferArray;

    public MemoryCachePage()
    {
        _bufferArray = ArrayPool<byte>.Shared.Rent(PAGE_SIZE);

        this.Buffer = new Memory<byte>(_bufferArray, 0, PAGE_SIZE);
    }

    public void Rent()
    {
        Interlocked.Increment(ref _sharedCounter);

        this.Timestamp = DateTime.UtcNow.Ticks;
    }

    public void Return()
    {
        Interlocked.Decrement(ref _sharedCounter);

        ENSURE(_sharedCounter < 0, "ShareCounter cached page must be large than 0");
    }

    public void Dispose()
    {
        ENSURE(_sharedCounter == 0, $"MemoryCachePage dispose with SharedCounter = {_sharedCounter}");

        ArrayPool<byte>.Shared.Return(_bufferArray);
    }
}
