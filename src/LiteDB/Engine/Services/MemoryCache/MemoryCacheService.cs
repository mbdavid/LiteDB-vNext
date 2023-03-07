namespace LiteDB.Engine;

// adding IDisposable in auto-generated interface IMemoryCacheService
internal partial interface IMemoryCacheService : IDisposable { }

/// <summary>
/// Page buffer cache. Keep a concurrent dictionary with buffers (byte[]) based on disk file position.
/// Cache both data and log pages. Works at buffer level
/// </summary>
[AutoInterface(true)]
internal class MemoryCacheService : IMemoryCacheService
{
    private readonly ConcurrentQueue<PageBuffer> _freePages = new();
    private int _pagesAllocated = 0;

    private ConcurrentDictionary<long, PageBuffer> _cache = new();

    public MemoryCacheService()
    {
    }

    /// <summary>
    /// Allocate, in memory, a new array with PAGE_SIZE inside a PageBuffer struct reference.
    /// </summary>
    public PageBuffer AllocateNewPage()
    {
        if (_freePages.TryDequeue(out var page))
        {
            return page;
        }

        var array = new byte[PAGE_SIZE];

        Interlocked.Increment(ref _pagesAllocated);

        return new PageBuffer(array);
    }

    public void DeallocatePage(PageBuffer buffer)
    {
        ENSURE(buffer.ShareCounter == 0, "ShareCounter must be 0 before return page to memory");
        ENSURE(!_cache.ContainsKey(buffer.Position), "PageBuffer must be outside cache");

        // clear buffer position/sharecounter
        buffer.Reset();

        // neste momento posso escolher se adiciono no _freePages
        _freePages.Enqueue(buffer);
    }

    /// <summary>
    /// Get a page from memory cache. If not exists, return null
    /// If exists, increase sharecounter (and must call Return() after use)
    /// </summary>
    public PageBuffer? GetPage(long position)
    {
        var found = _cache.TryGetValue(position, out PageBuffer page);

        if (found)
        {
            page.Rent();

            return page;
        }

        return null;
    }

    /// <summary>
    /// Add a new page to cache. Buffer must contains all data for postion in disk (data/log)
    /// </summary>
    public void AddPageInCache(PageBuffer page)
    {
        ENSURE(page.Position != long.MaxValue, "PageBuffer must have a position before add in cache");
        ENSURE(page.ShareCounter == 0, "ShareCounter must be zero before add in cache");

        var added = _cache.TryAdd(page.Position, page);

        ENSURE(!added, $"This page position {page.Position} already in cache");
    }

    /// <summary>
    /// Try remove page from cache based on shareCounter limit.
    /// </summary>
    public bool TryRemovePageFromCache(PageBuffer buffer, int shareCounter)
    {
        // if shareCounter from buffer are larger than shareCounter parameter, can't be removed from cache (is in use)
        if (buffer.ShareCounter > shareCounter) return false;

        // try delete this buffer
        var deleted = _cache.TryRemove(buffer.Position, out _);

        if (deleted)
        {
            // if after remove from cache, shareCounter was modified, re-add into cache
            if (buffer.ShareCounter > shareCounter)
            {
                var added = _cache.TryAdd(buffer.Position, buffer);

                ENSURE(added, "PageBuffer was already in cache after remove/re-add");

                return false;
            }

            // after remove this page from cache, clear Position/ShareCounter
            buffer.Reset();
        }

        return deleted;
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