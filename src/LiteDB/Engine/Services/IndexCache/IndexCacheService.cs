namespace LiteDB.Engine;

/// <summary>
/// Keep all read/
/// </summary>
[AutoInterface]
internal class IndexCacheService : IIndexCacheService
{
    //private ConcurrentDictionary<PageAddress, List<(int version, IndexNode node)> _cache = new();

    public IndexCacheService()
    {
    }

    /// <summary>
    /// Get index node from cache based on index address
    /// </summary>
    public IndexNode? GetIndexNode(PageAddress nodeAddress, int readVersion)
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
        // devo fazer de tudo no checkpoint? tenho como saber quais devem ser excluidos?
        return 0;
    }

}