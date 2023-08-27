using System.Collections.Generic;

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

    public int ItemsCount => _cache.Count;

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

        ENSURE(page.Header.TransactionID == 0, page);
        ENSURE(page.Header.IsConfirmed == false, page);

        // test if this page are getted from a writable collection in transaction
        writable = Array.IndexOf(writeCollections, page.Header.ColID) > -1;

        if (writable)
        {
            // if no one are using, remove from cache (double check)
            if (page.ShareCounter == 0)
            {
                var removed = _cache.TryRemove(positionID, out _);

                ENSURE(removed, new { removed, self = this });

                this.ClearPageWhenRemoveFromCache(page);

                return page;
            }
            else
            {
                // if page is in use, create a new page
                var newPage = _bufferFactory.AllocateNewPage();

                // copy all content for this new created page
                page.CopyBufferTo(newPage);

                // and return as a new page instance
                return newPage;
            }
        }
        else
        {
            // get page for read-only
            ENSURE(page.ShareCounter != NO_CACHE, page);

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
        // first try to remove from cache
        if (_cache.TryRemove(positionID, out PageBuffer cachePage))
        {
            //
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
        ENSURE(!page.IsDirty, "Page must be clean before add into cache", page);
        ENSURE(page.PositionID != int.MaxValue, "PageBuffer must have a position before add in cache", page);
        ENSURE(page.InCache == false, "ShareCounter must be zero before add in cache", page);

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

        // try add into cache before change page
        var added = _cache.TryAdd(page.PositionID, page);

        if (!added) return false;

        // clear any transaction info before add in cache
        page.Header.TransactionID = 0;
        page.Header.IsConfirmed = false;

        // initialize shared counter
        page.ShareCounter = 0;

        return true;
    }

    public void ReturnPageToCache(PageBuffer page)
    {
        Interlocked.Decrement(ref page.ShareCounter);

        ENSURE(page.ShareCounter >= 0, page);

        // clear header log information
        page.Header.TransactionID = 0;
        page.Header.IsConfirmed = false;
    }

    /// <summary>
    /// Set all variables to indicate that page are not in cache anymore 
    /// At this point, this PageBuffer are out of _cache dict
    /// </summary>
    private void ClearPageWhenRemoveFromCache(PageBuffer page)
    {
        ENSURE(page.IsDirty == false, page);
        ENSURE(page.ShareCounter == 0, $"Page should not be in use", page);
        ENSURE(page.Header.TransactionID == 0, page);
        ENSURE(page.Header.IsConfirmed == false, page);

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

    /// <summary>
    /// Remove from cache all logfile pages. Keeps only page that are from datafile. Used after checkpoint operation
    /// </summary>
    public void ClearLogPages()
    {
        var logPositions = _cache.Values
            .Where(x => x.IsDataFile)
            .Select(x => x.PositionID)
            .ToArray();

        foreach( var logPosition in logPositions)
        {
            _cache.Remove(logPosition, out var page);

            this.ClearPageWhenRemoveFromCache(page);

            _bufferFactory.DeallocatePage(page);
        }
    }

    public override string ToString()
    {
        return Dump.Object(new { _cache });
    }

    public void Dispose()
    {
        ENSURE(_cache.Count(x => x.Value.ShareCounter != 0) == 0, "Cache must be clean before dipose");

#if DEBUG
        // in DEBUG mode, let's clear all pages one-by-one
        foreach(var page in _cache.Values)
        {
            this.ClearPageWhenRemoveFromCache(page);
            _bufferFactory.DeallocatePage(page);
        }
#endif

        // deattach PageBuffers from _cache object
        _cache.Clear();
    }
}