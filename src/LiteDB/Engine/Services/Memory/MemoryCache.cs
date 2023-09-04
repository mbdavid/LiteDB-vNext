namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
unsafe internal class MemoryCache : IMemoryCache
{
    // dependency injection
    private readonly IMemoryFactory _memoryFactory;

    /// <summary>
    /// A dictionary to cache use/re-use same data buffer indexed by PositionID
    /// </summary>
    private readonly ConcurrentDictionary<uint, nint> _cache = new();

    public int ItemsCount => _cache.Count;

    public MemoryCache(IMemoryFactory memoryFactory)
    {
        _memoryFactory = memoryFactory;
    }

    public PageMemory* GetPageReadWrite(uint positionID, byte[] writeCollections, out bool writable, out bool found)
    {
        found = _cache.TryGetValue(positionID, out var ptr);

        if (!found)
        {
            writable = false;
            return null;
        }

        var page = (PageMemory*)ptr;

        ENSURE(page->ShareCounter != NO_CACHE);
        ENSURE(page->TransactionID == 0);
        ENSURE(page->IsConfirmed == false);

        // test if this page are getted from a writable collection in transaction
        writable = Array.IndexOf(writeCollections, page->ColID) > -1;

        if (writable)
        {
            // if no one are using, remove from cache (double check)
            if (page->ShareCounter == 0)
            {
                var removed = _cache.TryRemove(positionID, out _);

                ENSURE(removed, new { removed, self = this });

                // clean share counter after remove from cache
                page->ShareCounter = NO_CACHE;

                return page;
            }
            else
            {
                // if page is in use, create a new page
                var newPage = _memoryFactory.AllocateNewPage();

                // copy all content for this new created page
                PageMemory.CopyPageContent(page, newPage);

                // and return as a new page instance
                return newPage;
            }
        }
        else
        {
            // get page for read-only
            ENSURE(page->ShareCounter != NO_CACHE);

            // increment ShareCounter to be used by another transaction
            //Interlocked.Increment(ref pagePtr->ShareCounter);
            page->ShareCounter++;

            return page;
        }

        throw new NotSupportedException();
    }

    /// <summary>
    /// Remove page from cache. Must not be in use
    /// </summary>
    public bool TryRemove(uint positionID, [MaybeNullWhen(false)] out PageMemory* pagePtr)
    {
        // first try to remove from cache
        if (_cache.TryRemove(positionID, out var ptr))
        {
            pagePtr = (PageMemory*)ptr;

            pagePtr->ShareCounter = NO_CACHE;

            return true;
        }

        pagePtr = default;

        return false;
    }

    /// <summary>
    /// Add a new page to cache. Returns true if page was added. If returns false,
    /// page are not in cache and must be released in bufferFactory
    /// </summary>
    public bool AddPageInCache(PageMemory* page)
    {
        ENSURE(!page->IsDirty, "PageMemory must be clean before add into cache");
        ENSURE(page->PositionID != uint.MaxValue, "PageMemory must have a position before add in cache");
        ENSURE(page->ShareCounter == NO_CACHE);
        ENSURE(page->PageType == PageType.Data || page->PageType == PageType.Index);

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
        var added = _cache.TryAdd(page->PositionID, (nint)page);

        if (!added) return false;

        // clear any transaction info before add in cache
        page->TransactionID = 0;
        page->IsConfirmed = false;

        // initialize shared counter
        page->ShareCounter = 0;

        return true;
    }

    public void ReturnPageToCache(PageMemory* pagePtr)
    {
        ENSURE(!pagePtr->IsDirty);
        ENSURE(pagePtr->ShareCounter > 0 && pagePtr->ShareCounter < byte.MaxValue);

        //Interlocked.Decrement(ref page.ShareCounter);
        pagePtr->ShareCounter--;

        ENSURE(pagePtr->ShareCounter != NO_CACHE);

        // clear header log information
        pagePtr->TransactionID = 0;
        pagePtr->IsConfirmed = false;
    }

    /// <summary>
    /// Try remove pages with ShareCounter = 0 (not in use) and release this
    /// pages from cache. Returns how many pages was removed
    /// </summary>
    public int CleanUp()
    {
        var limit = (int)(CACHE_LIMIT * .3); // 30% of CACHE_LIMIT

        var positions = _cache.Values
            .Where(x => ((PageMemory*)x)->ShareCounter == 0)
//            .OrderByDescending(x => x.Timestamp)
            .Select(x => ((PageMemory*)x)->PositionID)
            .Take(limit)
            .ToArray();

        var total = 0;

        foreach(var positionID in positions)
        {
            var removed = _cache.TryRemove(positionID, out var ptr);

            if (!removed) continue;

            var pagePtr = (PageMemory*)ptr;

            // double check
            if (pagePtr->ShareCounter == 0)
            {
                // set page out of cache
                pagePtr->ShareCounter = NO_CACHE;
                pagePtr->TransactionID = 0;
                pagePtr->IsConfirmed = false;

                // deallocate page
                _memoryFactory.DeallocatePage(pagePtr);

                total++;
            }
            else
            {
                // page should be re-added to cache
                var added = _cache.TryAdd(positionID, ptr);

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
            .Where(x => ((PageMemory*)x)->PositionID != ((PageMemory*)x)->PageID)
            .Select(x => ((PageMemory*)x)->PositionID)
            .ToArray();

        foreach(var logPosition in logPositions)
        {
            _cache.Remove(logPosition, out var ptr);

            var pagePtr = (PageMemory*)ptr;

            pagePtr->ShareCounter = NO_CACHE;

            _memoryFactory.DeallocatePage(pagePtr);
        }
    }

    public override string ToString()
    {
        return Dump.Object(new { _cache });
    }

    public void Dispose()
    {
        ENSURE(_cache.Count(x => ((PageMemory*)x.Value) -> ShareCounter != 0) == 0, "Cache must be clean before dipose");

#if DEBUG
        // in DEBUG mode, let's clear all pages one-by-one
        foreach(var ptr in _cache.Values)
        {
            var pagePtr = (PageMemory*)ptr;
            pagePtr->ShareCounter = NO_CACHE;
            _memoryFactory.DeallocatePage(pagePtr);
        }
#endif

        // deattach PageBuffers from _cache object
        _cache.Clear();
    }
}