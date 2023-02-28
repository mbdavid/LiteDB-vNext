namespace LiteDB.Engine;

/// <summary>
/// Implement a page size fixed factory for data pages. Must dispose to dealocate memory
/// </summary>
internal class BufferPage : IMemoryOwner<byte>
{
    public static byte[] Empty { get; } = new byte[PAGE_SIZE];

    private readonly byte[] _source;

    private int _sharedCounter = 0;

    /// <summary>
    /// Position, in disk, where this page was read
    /// </summary>
    public long Position { get; }

    /// <summary>
    /// Contains how many threads are sharing this buffer slice for read. Used for cache service
    /// </summary>
    public int ShareCounter => _sharedCounter;

    /// <summary>
    /// Buffer page created or get from cache
    /// </summary>
    public long Timestamp { get; private set; } = DateTime.UtcNow.Ticks;

    public Memory<byte> Memory { get; }

    public BufferPage(bool clean)
    {
        _source = ArrayPool<byte>.Shared.Rent(PAGE_SIZE);

        this.Memory = new Memory<byte>(_source, 0, PAGE_SIZE);

        if (clean)
        {
            // update all page with 0
            this.Memory.Span.Fill(0);
        }
    }

    /// <summary>
    /// Checks if page buffer contains only 0
    /// </summary>
    public bool IsEmpty() => this.Memory.Span.IsFullZero();

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
        ArrayPool<byte>.Shared.Return(_source);
    }
}
