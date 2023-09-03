namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
unsafe internal class AllocationMapService : IAllocationMapService
{
    private readonly IAllocationMapPageModifier _pageModifier;
    private readonly IDiskService _diskService;
    private readonly IMemoryFactory _memoryFactory;

    /// <summary>
    /// List of all allocation map pages, in pageID order
    /// </summary>
    private readonly List<nint> _pages = new();

    public AllocationMapService(
        IAllocationMapPageModifier pageModifier,
        IDiskService diskService, 
        IMemoryFactory memoryFactory)
    {
        _pageModifier = pageModifier;
        _diskService = diskService;
        _memoryFactory = memoryFactory;
    }

    /// <summary>
    /// Initialize allocation map service loading all AM pages into memory and getting
    /// </summary>
    public void Initialize()
    {
        // read all allocation maps pages on disk
        var positionID = AM_FIRST_PAGE_ID;

        var writer = _diskService.GetDiskWriter();
        var lastPositionID = writer.GetLastFilePositionID();

        while (positionID <= lastPositionID)
        {
            var pagePtr = _memoryFactory.AllocateNewPage();

            writer.ReadPage(pagePtr, positionID);

            _pages.Add((nint)pagePtr);

            positionID += AM_PAGE_STEP;
        }
    }

    /// <summary>
    /// Get a free PageID based on colID/type. Create extend or new am page if needed. Return isNew if page are empty (must be initialized)
    /// </summary>
    public (uint pageID, bool isNew, ExtendLocation next) GetFreeExtend(ExtendLocation current, byte colID, PageType type)
    {
        var pagePtr = (PageMemory*)_pages[current.AllocationMapID];

        var (extendIndex, pageIndex, isNew) = _pageModifier.GetFreeExtend(pagePtr, current.ExtendIndex, colID, type);

        if (extendIndex >= 0)
        {
            var extend = new ExtendLocation(current.AllocationMapID, extendIndex);

            var pageID = (uint)(current.AllocationMapID * AM_PAGE_STEP + extendIndex * AM_EXTEND_SIZE + 1 + pageIndex);

            return (pageID, isNew, extend);
        }
        else if (extendIndex == -1 && current.AllocationMapID < _pages.Count - 1)
        {
            var next = new ExtendLocation(current.AllocationMapID + 1, 0);

            return this.GetFreeExtend(next, colID, type);
        }
        else
        {
            // create new extend map page
            var extend = new ExtendLocation(current.AllocationMapID + 1, 0);

            // if there is no more free extend in any AM page, let's create a new allocation map page
            var newPagePtr = _memoryFactory.AllocateNewPage();

            ENSURE(_pages.Count > 0);

            var lastPagePtr = (PageMemory*)_pages.Last();

            // get a new PageID based on last AM page
            var nextPageID = lastPagePtr->PageID + AM_PAGE_STEP;

            // get allocation map position
            newPagePtr->PositionID = nextPageID;
            newPagePtr->PageID = nextPageID;
            newPagePtr->PageType = PageType.AllocationMap;
            newPagePtr->IsDirty = true;

            _pages.Add((nint)newPagePtr);

            // call again this method with this new page
            return this.GetFreeExtend(extend, colID, type);
        }
    }

    /// <summary>
    /// Get an extend value from a extendID (global). This extendID should be already exists
    /// </summary>
    public uint GetExtendValue(ExtendLocation extend)
    {
        var pagePtr = (PageMemory*)_pages[extend.AllocationMapID];

        return pagePtr->Extends[extend.ExtendIndex];
    }

    /// <summary>
    /// Get PageMemory instance for a specific allocationMapID
    /// </summary>
    public PageMemory* GetPageMemory(int allocationMapID)
    {
        return (PageMemory*)_pages[allocationMapID];
    }

    /// <summary>
    /// Update allocation page map according with header page type and used bytes
    /// </summary>
    public void UpdatePageMap(uint pageID, ExtendPageValue pageValue)
    {
        var allocationMapID = (int)(pageID / AM_PAGE_STEP);
        var extendIndex = (int)((pageID - 1 - allocationMapID * AM_PAGE_STEP) / AM_EXTEND_SIZE);
        var pageIndex = (int)pageID - 1 - allocationMapID * AM_PAGE_STEP - extendIndex * AM_EXTEND_SIZE;

        var pagePtr = (PageMemory*)_pages[allocationMapID];

        _pageModifier.UpdateExtendPageValue(pagePtr, extendIndex, pageIndex, pageValue);
    }

    /// <summary>
    /// In a rollback error, should return all initial values to used extends
    /// </summary>
    public void RestoreExtendValues(IDictionary<int, uint> extendValues)
    {
        foreach(var extendValue in extendValues)
        {
            var extendLocation = new ExtendLocation(extendValue.Key);

            var pagePtr = (PageMemory*)_pages[extendLocation.AllocationMapID];

            pagePtr->Extends[extendLocation.ExtendIndex] = extendValue.Value;
        }
    }

    /// <summary>
    /// Write all dirty pages direct into disk (there is no log file to amp)
    /// </summary>
    public void WriteAllChanges()
    {
        var writer = _diskService.GetDiskWriter();

        foreach(var ptr in _pages)
        {
            var pagePtr = (PageMemory*)ptr;

            if (pagePtr->IsDirty)
            {
                writer.WritePage(pagePtr);
            }
        }
    }

    public override string ToString()
    {
        return Dump.Object(new { _pages = Dump.Array(_pages) });
    }

    public void Dispose()
    {

#if DEBUG
        // in DEBUG, let's deallocate all amp
        foreach(var ptr in _pages)
        {
            _memoryFactory.DeallocatePage((PageMemory*)ptr);
        }
#endif

        // clear list to be ready to use
        _pages.Clear();
    }
}