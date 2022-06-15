namespace LiteDB.Engine;

/// <summary>
/// Memory factory cache that pools memory allocation to reuse on pages access
/// </summary>
internal class MemoryCache
{
    private ConcurrentDictionary<long, MemoryCachePage> _cache = new();

    /// <summary>
    /// Get a page from memory cache. If not exists, return null
    /// If exists, increase sharecounter (and must call Return() after use)
    /// </summary>
    public MemoryCachePage GetPage(long position)
    {
        var page = _cache.GetOrAdd(position, p => new MemoryCachePage());

        page.Rent();

        return page;
    }

    public MemoryCachePage NewPage()
    {
        var page = new MemoryCachePage();

        page.Rent();

        return page;
    }

    public void AddPage(long position, MemoryCachePage page)
    {
        var added = _cache.TryAdd(position, page);

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