namespace LiteDB.Engine;

/// <summary>
/// Page buffer cache. Keep a concurrent dictionary with buffers (byte[]) based on disk file position.
/// Cache both data and log pages. Works at buffer level
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class CacheService : ICacheService
{
    // dependency injection
    private readonly IBufferFactory _bufferFactory;

    /// <summary>
    /// A dictionary to cache use/re-use same data buffer across threads. Rent model
    /// </summary>
    private readonly ConcurrentDictionary<int, PageBuffer> _cache = new();

    public CacheService(IBufferFactory bufferFactory)
    {
        _bufferFactory = bufferFactory;
    }

    /// <summary>
    /// Get a page from memory cache. If not exists, return null
    /// If exists, increase sharecounter (and must call Return() after use)
    /// </summary>
    public PageBuffer? GetPageRead(int positionID)
    {
        var found = _cache.TryGetValue(positionID, out PageBuffer page);

        if (!found) return null;

        ENSURE(page.ShareCounter != NO_CACHE, $"rent page {page} only for cache");

        // increment ShareCounter to be used by another transaction
        Interlocked.Increment(ref page.ShareCounter);

        page.Timestamp = DateTime.UtcNow.Ticks;

        return page;
    }

    /// <summary>
    /// Get a page from memory cache. If not exists, return null
    /// If exists, checks if ShareCounter == 0, remove from _cache and returns.
    /// If ShareCounter > 0, create a new PageBuffer with copied content
    /// </summary>
    public PageBuffer? GetPageWrite(int positionID)
    {
        // try find page using remove
        var found = _cache.TryRemove(positionID, out PageBuffer page);

        if (!found) return null;

        // if no one are using, reset page and returns
        if (page.ShareCounter == 0)
        {
            page.Reset();

            return page;
        }
        else
        {
            // if page is in use, create a new page
            var newPage = _bufferFactory.AllocateNewPage(false);

            // copy all content for this new created page
            page.CopyBufferTo(newPage);

            // re-add page on cache
            var added = _cache.TryAdd(positionID, page);

            if (!added)
            {
                throw new NotImplementedException("problema de concorrencia. não posso descartar paginas.. como fazer? manter em lista paralela?");
            }

            return newPage;
        }
    }

    /// <summary>
    /// Remove page from cache. Must not be in use
    /// </summary>
    public PageBuffer? TryRemove(int positionID)
    {
        if (_cache.TryRemove(positionID, out PageBuffer page))
        {
            ENSURE(page.ShareCounter == 0, $"page {page} should not be in use");

            // reset page (not in cache anymore)
            page.Reset();

            return page;
        }

        return null;
    }

    /// <summary>
    /// Add a new page to cache. Returns true if page was added. If returns false,
    /// page are not in cache and must be released in bufferFactory
    /// </summary>
    public bool AddPageInCache(PageBuffer page)
    {
        ENSURE(page.PositionID != int.MaxValue, "PageBuffer must have a position before add in cache");
        ENSURE(page.ShareCounter == NO_CACHE, "ShareCounter must be zero before add in cache");

        // before add, checks cache limit and cleanup if full
        if (_cache.Count >= CACHE_LIMIT)
        {
            var clean = this.CleanUp();

            // all pages are in use, do not add this page in cache (cache full used)
            if (clean == 0)
            {
                return false;
            }
        }

        page.ShareCounter = 0; // initialize share counter

        var added = _cache.TryAdd(page.PositionID, page);

        return added;
    }

    public void ReturnPage(PageBuffer page)
    {
        Interlocked.Decrement(ref page.ShareCounter);

        ENSURE(page.ShareCounter < 0, $"ShareCounter cached page {page} must be large than 0");
    }

    /// <summary>
    /// Try remove pages with ShareCounter = 0 (not in use) and release this
    /// pages from cache. Returns how many pages was removed
    /// </summary>
    public int CleanUp()
    {
        var limit = (int)(CACHE_LIMIT * .3); // 30% of CACHE_LIMIT

        var positions = _cache.Values
            .Where(x => x.ShareCounter == 0)
            .OrderByDescending(x => x.Timestamp)
            .Select(x => x.PositionID)
            .Take(limit)
            .ToArray();

        var total = 0;

        foreach(var positionID in positions)
        {
            var removed = _cache.TryRemove(positionID, out var page);

            if (!removed) continue;

            // 
            if (page.ShareCounter == 0)
            {
                // set page out of cache
                page.Reset();

                // deallocate page
                _bufferFactory.DeallocatePage(page);

                total++;
            }
            else
            {
                // page should be re-added to cache
                var added = _cache.TryAdd(positionID, page);

                if (!added)
                {
                    throw new NotImplementedException("problema de concorrencia. não posso descartar paginas.. como fazer? manter em lista paralela?");
                }
            }
        }

        return total;
    }

    public void Dispose()
    {
        ENSURE(_cache.Count(x => x.Value.ShareCounter != 0) == 0, "cache must be clean before dipose");


        // deattach PageBuffers from _cache object
        _cache.Clear();
    }
}