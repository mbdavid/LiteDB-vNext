namespace LiteDB.Engine;

/// <summary>
/// Memory factory cache that pools memory allocation to reuse on pages access
/// </summary>
[AutoInterface(true)]
internal class IndexCacheService : IIndexCacheService
{
    //private ConcurrentDictionary<string, <(int version, PageAddress addr)> _cache = new();

    public IndexCacheService()
    {
    }

    /// <summary>
    /// Get index node from cache based on index address
    /// </summary>
    public IndexNode GetIndexNode(PageAddress nodeAddress, int readVersion)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// </summary>
    public void AddIndexNode(IndexNode indexNode, int version)
    {
        // pode chegar num momento que não adiciono mais por falta de memoria. Plugin?
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