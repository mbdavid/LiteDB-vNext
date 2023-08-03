namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class AllocationMapService : IAllocationMapService
{
    private readonly IDiskService _diskService;
    private readonly IBufferFactory _bufferFactory;

    /// <summary>
    /// List of all allocation map pages, in pageID order
    /// </summary>
    private readonly List<AllocationMapPage> _pages = new();

    /// <summary>
    /// </summary>
    private readonly AllocationMapSession[] _sessions = new AllocationMapSession[byte.MaxValue + 1];

    public AllocationMapService(
        IDiskService diskService, 
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
            var page = new AllocationMapPage(pageBuffer);

            // add AM page to instance
            _pages.Add(page);
        }
    }

    /// <summary>
    /// Get session allocation map for a collection. Reuse same session instance for each collection
    /// </summary>
    public AllocationMapSession GetSession(byte colID)
    {
        var session = _sessions[colID] ??= new AllocationMapSession(colID);

        return session;
    }

    /// <summary>
    /// Read all allocation map pages. Allocation map pages contains initial position and fixed interval between other pages
    /// </summary>
    private async IAsyncEnumerable<PageBuffer> ReadAllocationMapPages()
    {
        var positionID = AM_FIRST_PAGE_ID;

        var writer = _diskService.GetDiskWriter();
        var lastPositionID = writer.GetLastFilePositionID();

        while (positionID <= lastPositionID)
        {
            var page = _bufferFactory.AllocateNewPage(false);

            await writer.ReadPageAsync(positionID, page);

            //TODO: verificar se ta certo
            if (page.IsHeaderEmpty())
            {
                _bufferFactory.DeallocatePage(page);
                break;
            }

            yield return page;

            positionID += AM_PAGE_STEP;
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
            if (page.Page.IsDirty)
            {
                await writer.WritePageAsync(page.Page);
            }
        }
    }

    /// <summary>
    /// </summary>
    public ExtendLocation CreateNewExtend(byte colID)
    {
        //TODO: lock, pois não pode ter 2 threads aqui


        // try create extend in all AM pages already exists
        foreach (var page in _pages)
        {
            // create new extend on page (if this page contains empty extends)
            var extendIndex = page.CreateNewExtend(colID);
            
            if (extendIndex >= 0) 
            {
                // return first empty page
                return new(page.AllocationMapID, extendIndex);
            }
        }

        // if there is no more free extend in any AM page, let's create a new allocation map page
        var pageBuffer = _bufferFactory.AllocateNewPage(true);

        // get a new PageID based on last AM page
        var nextPageID = _pages.Last().Page.Header.PageID + AM_PAGE_STEP;

        // create new AM page and add to list
        var newPage = new AllocationMapPage(nextPageID, pageBuffer);

        _pages.Add(newPage);

        // create new extend for this collection - always return true because it´s a new page
        var newExtendIndex = newPage.CreateNewExtend(colID);

        // return this new extend location
        return new(newPage.AllocationMapID, newExtendIndex);
    }

    public void Dispose()
    {
        // deallocate all amp
        foreach(var page in _pages)
        {
            _bufferFactory.DeallocatePage(page.Page);
        }

        // clear list to be ready to use
        _pages.Clear();
    }
}