namespace LiteDB.Engine;

/// <summary>
/// </summary>
internal class AllocationMapService : IDisposable
{
    private List<AllocationMapPage> _pages = new();

    public void Update(uint pageID, PageType pageType, byte colID, ushort freeSpace)
    {
        // testa pageType e colID no ENSURE
    }

    public uint GetFreePage(byte coldID, PageType type, int length)
    {
        // busca ou cria uma nova (tanto novo extend como nova allocation page)!
        return 0;
    }


    /// <summary>
    /// Get a IEnumerable with all changed pages to write on disk in Shutdown process
    /// </summary>
    public IEnumerable<Memory<byte>> GetDirtyPages()
    {
        foreach(var page in _pages)
        {
            if (page.IsDirty)
            {
                yield return page.GetBufferWrite();
            }
        }
    }

    public void Dispose()
    {
        foreach(var page in _pages)
        {
            page.Dispose();
        }
    }
}
