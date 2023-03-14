namespace LiteDB.Engine;

/// <summary>
/// * Shared (thread safe)
/// </summary>
internal struct PageBuffer
{
    public readonly byte[] Array;

    /// <summary>
    /// Position on disk where this page came from or where this page must be stored
    /// </summary>
    public long Position;

    /// <summary>
    /// Contains how many threads are sharing this buffer slice for read. Used for cache service
    /// </summary>
    public int ShareCounter;

    /// <summary>
    /// Last time this buffer was hit by cache
    /// </summary>
    public long Timestamp;

    public PageBuffer(byte[] array)
    {
        this.Array = array;
        this.Position = long.MaxValue;
        this.ShareCounter = 0;
        this.Timestamp = 0;
    }

    public void Reset()
    {
        this.ShareCounter = 0;
        this.Timestamp = 0;
        this.Position = long.MaxValue;
    }

    public Span<byte> AsSpan()
    {
        return this.Array.AsSpan(0, PAGE_SIZE);
    }

    public void Rent()
    {
        Interlocked.Increment(ref this.ShareCounter);

        this.Timestamp = DateTime.UtcNow.Ticks;
    }

    public void Return()
    {
        Interlocked.Decrement(ref this.ShareCounter);

        ENSURE(this.ShareCounter < 0, "ShareCounter cached page must be large than 0");
    }
}
