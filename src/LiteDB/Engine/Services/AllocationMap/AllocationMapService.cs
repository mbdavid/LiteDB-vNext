namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface]
internal class AllocationMapService : IAllocationMapService
{
    private readonly IServicesFactory _factory;
    private List<AllocationMapPage> _pages = new();

    /// <summary>
    /// A struct, per colID, to store a list of pages with available space
    /// </summary>
    private readonly CollectionFreePages[] _collectionFreePages = new CollectionFreePages[byte.MaxValue];

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
    /// Return pageID and bool to indicate that this page is a new empty page (must be created)
    /// </summary>  
    public (uint, bool) GetFreePageID(byte coldID, PageType type, int length)
    {
        var freePages = _collectionFreePages[coldID];

        if (type == PageType.Data)
        {
            if (length >= AMP_DATA_PAGE_SPACE_001 && freePages.DataPages_001.Count > 0)
            {
                return (freePages.DataPages_001.First(), false);
            }
            if (length >= AMP_DATA_PAGE_SPACE_010 && freePages.DataPages_001.Count > 0)
            {
                return (freePages.DataPages_001.First(), false);
            }
        }


        return (150, true);
    }

}