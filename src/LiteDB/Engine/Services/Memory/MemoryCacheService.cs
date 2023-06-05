namespace LiteDB.Engine;

/// <summary>
/// Page buffer cache. Keep a concurrent dictionary with buffers (byte[]) based on disk file position.
/// Cache both data and log pages. Works at buffer level
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class MemoryCacheService : IMemoryCacheService
{
    /// <summary>
    /// A dictionary to cache use/re-use same data buffer across threads. Rent model
    /// </summary>
    private ConcurrentDictionary<int, PageBuffer> _cache = new();

    public MemoryCacheService()
    {
    }

    /// <summary>
    /// Get a page from memory cache. If not exists, return null
    /// If exists, increase sharecounter (and must call Return() after use)
    /// </summary>
    public PageBuffer? GetPage(int positionID)
    {
        var found = _cache.TryGetValue(positionID, out PageBuffer page);

        if (found)
        {
            ENSURE(page.ShareCounter != PAGE_NO_CACHE, $"rent page {page} only for cache");

            // increment ShareCounter to be used by another transaction
            Interlocked.Increment(ref page.ShareCounter);

            page.Timestamp = DateTime.UtcNow.Ticks;

            return page;
        }

        return null;
    }

    /// <summary>
    /// Remove page from cache. Must not be in use
    /// </summary>
    public PageBuffer? TryRemove(int positionID)
    {
        if (_cache.TryRemove(positionID, out PageBuffer page))
        {
            ENSURE(page.ShareCounter == 0, $"page {page} should not be in use");

            return page;
        }

        return null;
    }

    /// <summary>
    /// Add a new page to cache
    /// </summary>
    public bool AddPageInCache(PageBuffer page)
    {
        ENSURE(page.PositionID != int.MaxValue, "PageBuffer must have a position before add in cache");
        ENSURE(page.ShareCounter == PAGE_NO_CACHE, "ShareCounter must be zero before add in cache");

        page.ShareCounter = 0; // initialize share counter

        var added = _cache.TryAdd(page.PositionID, page);

        return added;
    }

    /// <summary>
    /// Try remove page from cache based on shareCounter limit.
    /// </summary>
    public bool TryRemovePageFromCache(PageBuffer page, int maxShareCounter)
    {
        var pageShareCounter = page.ShareCounter;

        // if shareCounter from buffer are larger than shareCounter parameter, can't be removed from cache (is in use)
        if (pageShareCounter > maxShareCounter) return false;

        // try delete this buffer
        var deleted = _cache.TryRemove(page.PositionID, out _);

        if (deleted)
        {
            // if after remove from cache, shareCounter was modified, re-add into cache
            if (pageShareCounter != page.ShareCounter)
            {
                var added = _cache.TryAdd(page.PositionID, page);

                ENSURE(added, "PageBuffer was already in cache after remove/re-add");

                return false;
            }

            // after remove this page from cache, clear Position/ShareCounter
            page.Reset();
        }

        return deleted;
    }

    public void ReturnPage(PageBuffer page)
    {
        Interlocked.Decrement(ref page.ShareCounter);

        ENSURE(page.ShareCounter < 0, $"ShareCounter cached page {page} must be large than 0");

    }

    public int CleanUp()
    {


        // faz um for e limpa toda _cache que tiver shared = 0 (chama dispose)
        // retorna quantas paginas estão na _cache ainda
        // não precisa de lock exclusivo para rodar
        // faz gum GC
        return 0;
    }

    public void Dispose()
    {
    }
}