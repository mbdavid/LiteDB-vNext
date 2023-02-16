namespace LiteDB.Engine;

/// <summary>
/// Memory factory cache that pools memory allocation to reuse on pages access
/// </summary>
//[GenerateAutoInterface]
internal class MemoryCacheService //: IMemoryCacheService
{
    private ConcurrentDictionary<long, MemoryCachePage> _cache = new();

    public MemoryCacheService()
    {
    }

    /// <summary>
    /// Get a page from memory cache. If not exists, return null
    /// If exists, increase sharecounter (and must call Return() after use)
    /// </summary>
    public MemoryCachePage GetPage(long position)
    {
        var found = _cache.TryGetValue(position, out MemoryCachePage page);

        if (found)
        {
            page.Rent();

            return page;
        }

        return null;
    }

    /// <summary>
    /// Return a page to cache after a GetPage
    /// </summary>
    public void ReturnPage(long position)
    {
        var found = _cache.TryGetValue(position, out MemoryCachePage page);

        ENSURE(!found, $"This page position {position} are not in cache");

        page.Return();
    }

    /// <summary>
    /// Add a new page to cache. Must have unique Position. Page must be clean 
    /// </summary>
    public void AddPage(long position, BasePage page)
    {
        ENSURE(page.IsDirty == false, "Page must be clean to be added on cache");

        var cached = new MemoryCachePage(page);

        var added = _cache.TryAdd(position, cached);

        ENSURE(!added, $"This page position {position} already in memory cache");
    }

    public int CleanUp()
    {
        // faz um for e limpa toda _cache que tiver shared = 0 (chama dispose)
        // retorna quantas paginas estão na _cache ainda
        // não precisa de lock exclusivo para rodar
        // faz gum GC
        return 0;
    }

}