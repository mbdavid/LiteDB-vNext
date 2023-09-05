﻿namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
[Obsolete]
internal class __AllocationMapService : I__AllocationMapService
{
    private readonly I__DiskService _diskService;
    private readonly IBufferFactory _bufferFactory;

    /// <summary>
    /// List of all allocation map pages, in pageID order
    /// </summary>
    private readonly List<__AllocationMapPage> _pages = new();

    public __AllocationMapService(
        I__DiskService diskService, 
        IBufferFactory bufferFactory)
    {
        _diskService = diskService;
        _bufferFactory = bufferFactory;
    }

    /// <summary>
    /// Initialize allocation map service loading all AM pages into memory and getting
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        // read all allocation maps pages on disk
        await foreach (var pageBuffer in this.ReadAllocationMapPages())
        {
            // read all page buffer into an int array
            var page = new __AllocationMapPage(pageBuffer);

            // add AM page to instance
            _pages.Add(page);
        }
    }

    /// <summary>
    /// Read all allocation map pages. Allocation map pages contains initial position and fixed interval between other pages
    /// </summary>
    private async IAsyncEnumerable<PageBuffer> ReadAllocationMapPages()
    {
        var positionID = __AM_FIRST_PAGE_ID;

        var writer = _diskService.GetDiskWriter();
        var lastPositionID = writer.GetLastFilePositionID();

        while (positionID <= lastPositionID)
        {
            var page = _bufferFactory.AllocateNewPage();

            await writer.ReadPageAsync(positionID, page);

            ENSURE(!page.IsHeaderEmpty(), page);

            yield return page;

            positionID += AM_PAGE_STEP;
        }
    }

    /// <summary>
    /// Get a free PageID based on colID/type. Create extend or new am page if needed. Return isNew if page are empty (must be initialized)
    /// </summary>
    public (int pageID, bool isNew, ExtendLocation next) GetFreeExtend(ExtendLocation current, byte colID, PageType type)
    {
        var page = _pages[current.AllocationMapID];

        var (extendIndex, pageIndex, isNew) = page.GetFreeExtend(current.ExtendIndex, colID, type);

        if (extendIndex >= 0)
        {
            var extend = new ExtendLocation(current.AllocationMapID, extendIndex);

            var pageID = page.AllocationMapID * AM_PAGE_STEP + extendIndex * AM_EXTEND_SIZE + 1 + pageIndex;

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
            var mapPageBuffer = _bufferFactory.AllocateNewPage();

            // get a new PageID based on last AM page
            var nextPageID = _pages.Last().Page.Header.PageID + AM_PAGE_STEP;

            // get allocation map position
            mapPageBuffer.PositionID = nextPageID;

            // create new AM page and add to list
            var newPage = new __AllocationMapPage(nextPageID, mapPageBuffer);

            _pages.Add(newPage);

            // call again this method with this new page
            return this.GetFreeExtend(extend, colID, type);
        }
    }

    /// <summary>
    /// Get an extend value from a extendID (global). This extendID should be already exists
    /// </summary>
    public uint GetExtendValue(ExtendLocation extend)
    {
        var page = _pages[extend.AllocationMapID];

        return page.GetExtendValue(extend.ExtendIndex);
    }

    /// <summary>
    /// Get PageBuffer instance for a specific allocationMapID
    /// </summary>
    public PageBuffer GetPageBuffer(int allocationMapID)
    {
        return _pages[allocationMapID].Page;
    }

    /// <summary>
    /// Update allocation page map according with header page type and used bytes
    /// </summary>
    public void UpdatePageMap(int pageID, ExtendPageValue pageValue)
    {
        var allocationMapID = (int)(pageID / AM_PAGE_STEP);
        var extendIndex = (pageID - 1 - allocationMapID * AM_PAGE_STEP) / AM_EXTEND_SIZE;
        var pageIndex = pageID - 1 - allocationMapID * AM_PAGE_STEP - extendIndex * AM_EXTEND_SIZE;

        var page = _pages[allocationMapID];

        page.UpdateExtendPageValue(extendIndex, pageIndex, pageValue);
    }

    /// <summary>
    /// In a rollback error, should return all initial values to used extends
    /// </summary>
    public void RestoreExtendValues(IDictionary<int, uint> extendValues)
    {
        foreach(var extendValue in extendValues)
        {
            var extendLocation = new ExtendLocation(extendValue.Key);

            var page = _pages[extendLocation.AllocationMapID];

            page.RestoreExtendValue(extendLocation.ExtendIndex, extendValue.Value);
        }
    }

    /// <summary>
    /// Write all dirty pages direct into disk (there is no log file to amp)
    /// </summary>
    public async ValueTask WriteAllChangesAsync()
    {
        var writer = _diskService.GetDiskWriter();

        foreach(var page in _pages)
        {
            if (page.UpdatePageBuffer())
            {
                await writer.WritePageAsync(page.Page);
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
        foreach(var page in _pages)
        {
            _bufferFactory.DeallocatePage(page.Page);
        }
#endif

        // clear list to be ready to use
        _pages.Clear();
    }
}