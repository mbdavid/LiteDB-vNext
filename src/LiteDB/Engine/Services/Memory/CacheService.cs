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

    public PageBuffer? GetPageReadWrite(int positionID, byte[] writeCollections, out bool writable)
    {
        var found = _cache.TryGetValue(positionID, out PageBuffer page);

        if (!found)
        {
            writable = false;
            return null;
        }

        // test if this page are getted from a writable collection in transaction
        writable = Array.IndexOf(writeCollections, page.Header.ColID) > -1;

        if (writable)
        {
            // if no one are using, remove from cache (double check)
            if (page.ShareCounter == 0)
            {
                var removed = _cache.TryRemove(positionID, out _);

                ENSURE(() => removed);

                this.ClearPageWhenRemoveFromCache(page);

                return page;
            }
            else
            {
                // if page is in use, create a new page
                var newPage = _bufferFactory.AllocateNewPage(false);

                // copy all content for this new created page
                page.CopyBufferTo(newPage);

                // and return as a new page instance
                return newPage;
            }
        }
        else
        {
            // get page for read-only
            ENSURE(() => page.ShareCounter != NO_CACHE);

            // increment ShareCounter to be used by another transaction
            Interlocked.Increment(ref page.ShareCounter);

            page.Timestamp = DateTime.UtcNow.Ticks;

            return page;
        }

        throw new NotSupportedException();
    }

    /// <summary>
    /// Remove page from cache. Must not be in use
    /// </summary>
    public bool TryRemove(int positionID, [MaybeNullWhen(false)] out PageBuffer? page)
    {
        if (_cache.TryRemove(positionID, out PageBuffer cachePage))
        {
            this.ClearPageWhenRemoveFromCache(cachePage);

            page = cachePage;

            return true;
        }

        page = default;

        return false;
    }

    /// <summary>
    /// Add a new page to cache. Returns true if page was added. If returns false,
    /// page are not in cache and must be released in bufferFactory
    /// </summary>
    public bool AddPageInCache(PageBuffer page)
    {
        ENSURE(() => !page.IsDirty, "Page must be clean before add into cache");
        ENSURE(() => page.PositionID != int.MaxValue, "PageBuffer must have a position before add in cache");
        ENSURE(() => page.InCache == false, "ShareCounter must be zero before add in cache");

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

        ENSURE(() => page.ShareCounter >= 0);
    }

    /// <summary>
    /// Set all variables to indicate that page are not in cache anymore 
    /// At this point, this PageBuffer are only out of _cache
    /// </summary>
    private void ClearPageWhenRemoveFromCache(PageBuffer page)
    {
        ENSURE(() => page.IsDirty == false);
        ENSURE(() => page.ShareCounter == 0, $"Page should not be in use");

        page.ShareCounter = NO_CACHE;
        page.Timestamp = 0;
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

    public override string ToString()
    {
        return $"Cached pages: {_cache.Count}";
    }

    public void Dispose()
    {
        ENSURE(() => _cache.Count(x => x.Value.ShareCounter != 0) == 0, "Cache must be clean before dipose");

        // deattach PageBuffers from _cache object
        _cache.Clear();
    }
}