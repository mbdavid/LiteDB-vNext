namespace LiteDB.Engine;

/// <summary>
/// Each memory cache page represent a shared buffer with PAGE_SIZE. 
/// Implements IDisposable when page
/// </summary>
internal class MemoryCachePage
{
    /// <summary>
    /// Contains how many people are sharing this page for read
    /// </summary>
    private int _sharedCounter = 0;

    public int ShareCounter => _sharedCounter;
    public long Timestamp { get; private set; } = DateTime.UtcNow.Ticks;
    public BasePage Page { get; }

    public MemoryCachePage(BasePage page)
    {
        this.Page = page;
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
}
