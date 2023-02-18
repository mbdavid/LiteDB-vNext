namespace LiteDB.Engine;

/// <summary>
/// Each memory cache page represent a shared buffer with PAGE_SIZE. 
/// Implements IDisposable when page
/// </summary>
internal class PageCacheItem
{
    /// <summary>
    /// Contains how many people are sharing this page for read
    /// </summary>
    private int _sharedCounter = 0;

    public int ShareCounter => _sharedCounter;
    public long Timestamp { get; private set; } = DateTime.UtcNow.Ticks;
    public Memory<byte> Buffer { get; }

    public PageCacheItem()
    {
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

    public bool MarkAsWritable()
    {
        //TODO: implementar com exchange? precisa ser atomico
        if (_sharedCounter == 1)
        {
            _sharedCounter = -1;
            return true;
        }
        return false;
    }
}
