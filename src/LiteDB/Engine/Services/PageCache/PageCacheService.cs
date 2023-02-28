namespace LiteDB.Engine;

/// <summary>
/// Page buffer cache. Keep a concurrent dictionary with buffers (byte[]) based on disk file position.
/// Cache both data and log pages. Works at buffer level
/// </summary>
[AutoInterface(true)]
internal class PageCacheService : IPageCacheService
{
    private ConcurrentDictionary<long, BufferPage> _cache = new();

    public PageCacheService()
    {
    }

    /// <summary>
    /// Get a page from memory cache. If not exists, return null
    /// If exists, increase sharecounter (and must call Return() after use)
    /// </summary>
    public BufferPage? GetBufferPage(long position)
    {
        var found = _cache.TryGetValue(position, out BufferPage page);

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
    public void AddPage(PageCacheItem page)
    {
        var added = _cache.TryAdd(page.Position, page);

        ENSURE(!added, $"This page position {page.Position} already in memory cache");
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