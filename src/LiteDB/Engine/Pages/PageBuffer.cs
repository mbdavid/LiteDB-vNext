namespace LiteDB.Engine;

/// <summary>
/// * Shared (thread safe)
/// </summary>
internal struct PageBuffer
{
    /// <summary>
    /// Position on disk where this page came from or where this page must be stored
    /// </summary>
    public long Position = long.MaxValue;

    /// <summary>
    /// Contains how many threads are sharing this buffer slice for read. Used for cache service
    /// </summary>
    public int ShareCounter = 0;

    /// <summary>
    /// Last time this buffer was hit by cache
    /// </summary>
    public long Timestamp = 0;

    /// <summary>
    /// Get/Set if page was modified and need to saved on disk
    /// </summary>
    public bool IsDirty = false;

    /// <summary>
    /// Page header structure. Must be loaded/updated to buffer 
    /// </summary>
    public PageHeader Header = new();

    /// <summary>
    /// Page memory buffer with PAGE_SIZE size
    /// </summary>
    public readonly byte[] Buffer = new byte[PAGE_SIZE];

    public PageBuffer()
    {
    }

    public void Reset()
    {
        this.ShareCounter = 0;
        this.Timestamp = 0;
        this.Position = long.MaxValue;
        this.IsDirty = false;
    }

    public Span<byte> AsSpan()
    {
        return this.Buffer.AsSpan(0, PAGE_SIZE);
    }

    public Span<byte> AsSpan(int start)
    {
        return this.Buffer.AsSpan(start);
    }

    public Span<byte> AsSpan(int start, int length)
    {
        return this.Buffer.AsSpan(start, length);
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

    /// <summary>
    /// Test if first 32 header bytes are zero
    /// </summary>
    public bool IsHeaderEmpty()
    {
        return this.Buffer.AsSpan()[0..(PAGE_HEADER_SIZE - 1)].IsFullZero();
    }
}
