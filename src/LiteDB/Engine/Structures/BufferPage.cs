using System.Net.NetworkInformation;

namespace LiteDB.Engine;

/// <summary>
/// Implement a page size fixed factory for data pages. Must dispose to dealocate memory
/// </summary>
internal class BufferPage
{
    private readonly IMemoryOwner<byte> _source;

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

    public Memory<byte> Memory => _source.Memory;

    public BufferPage(IMemoryOwner<byte> source)
    {
        _source = source;
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
        ENSURE(_sharedCounter == 0, "ShareCounter must be zero when dispose BufferPage");

        _source.Dispose();
    }
}
