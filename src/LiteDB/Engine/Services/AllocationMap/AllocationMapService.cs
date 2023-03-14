namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface]
internal class AllocationMapService : IAllocationMapService
{
    private readonly IServicesFactory _factory;
    private List<AllocationMapPage> _pages = new();

    public AllocationMapService(IServicesFactory factory)
    {
        _factory = factory;
    }

    public async Task<bool> Initialize()
    {
        return true;
    }

    /// <summary>
    /// Return a page ID with space avaiable to store length bytes. Support only DataPages and IndexPages.
    /// Can return a existing page ID or an EmptyPage (new page)
    /// </summary>
    public uint GetFreePageID(byte coldID, PageType type, int length)
    {
        return 0;
    }

}