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
    /// A struct, per colID (0-255), to store a list of pages with available space
    /// </summary>
    private readonly CollectionFreePages[] _collectionFreePages;

    public AllocationMapService(
        IDiskService diskService, 
        IBufferFactory bufferFactory)
    {
        _diskService = diskService;
        _bufferFactory = bufferFactory;

        _collectionFreePages = new CollectionFreePages[byte.MaxValue + 1];
    }

    /// <summary>
    /// Initialize allocation map service loading all AM pages into memory and getting
    /// </summary>
    public async Task InitializeAsync()
    {
        // read all allocation maps pages on disk
        await foreach (var pageBuffer in this.ReadAllocationMapPages())
        {
            // get page buffer from disk
            var page = new AllocationMapPage(pageBuffer);

            // read all collection map in memory
            page.ReadAllocationMap(_collectionFreePages);

            // add AM page to instance
            _pages.Add(page);
        }
    }


    /// <summary>
    /// Read all allocation map pages. Allocation map pages contains initial position and fixed interval between other pages
    /// </summary>
    private async IAsyncEnumerable<PageBuffer> ReadAllocationMapPages()
    {
        var positionID = AM_FIRST_PAGE_ID;

        var lastPositionID = _diskService.GetLastFilePositionID();
        var writer = _diskService.GetDiskWriter();

        while (positionID <= lastPositionID)
        {
            var page = _bufferFactory.AllocateNewPage(false);

            await writer.ReadPageAsync(positionID, page);

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
    public async Task WriteAllChangesAsync()
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
    /// Return a page ID with space available to store 'length' bytes. Support only DataPages and IndexPages.
    /// Return pageID and a bool that indicates if this page is a new empty page (must be created)
    /// </summary>  
    public (int, bool) GetFreePageID(byte colID, PageType type, int length)
    {
        // get (or create) collection free pages for this colID
        var freePages = _collectionFreePages[colID] ??= new CollectionFreePages();

        if (type == PageType.Data)
        {
            // test if length for SMALL size document length
            if (length < AM_DATA_PAGE_SPACE_SMALL)
            {
                if (freePages.DataPagesSmall.Count > 0) // test for small bucket
                {
                    return (freePages.DataPagesSmall.Dequeue(), false);
                }
                else if (freePages.DataPagesMedium.Count > 0) // test in medium bucket
                {
                    return (freePages.DataPagesMedium.Dequeue(), false);
                }
                else if (freePages.DataPagesLarge.Count > 0) // test in large bucket
                {
                    return (freePages.DataPagesLarge.Dequeue(), false);
                }
            }

            // test if length for MEDIUM size document length
            else if (length < AM_DATA_PAGE_SPACE_MEDIUM)
            {
                if (freePages.DataPagesMedium.Count > 0) // test in medium bucket
                {
                    return (freePages.DataPagesMedium.Dequeue(), false);
                }
                else if (freePages.DataPagesLarge.Count > 0) // test for large bucket
                {
                    return (freePages.DataPagesLarge.Dequeue(), false);
                }
            }

            // test if length for LARGE size document length (considering 1 page block)
            else if (length < AM_DATA_PAGE_SPACE_LARGE)
            {
                if (freePages.DataPagesLarge.Count > 0)
                {
                    return (freePages.DataPagesLarge.Dequeue(), false);
                }
            }
        }
        else // PageType = IndexPage
        {
            if (freePages.IndexPages.Count > 0)
            {
                return (freePages.IndexPages.Dequeue(), false);
            }
        }

        //TODO: nesse ponto eu poderia tentar dar um "Reload" na freePages pra carregar mais (se tiver mais)
        // HasMore = true??

        // there is no page available with a best fit - create a new page
        if (freePages.EmptyPages.Count > 0)
        {
            return (freePages.EmptyPages.Dequeue(), true);
        }

        // if there is no empty pages, create new extend for this collection with new 8 pages
        var emptyPageID = this.CreateNewExtend(colID, freePages);

        return (emptyPageID, true);
    }

    /// <summary>
    /// Create a new extend in any allocation map page that contains space available. If all pages are full, create another allocation map page
    /// Return the first empty pageID created for this collection in this new extend
    /// This method populate collectionFreePages[colID] with 8 new empty pages
    /// </summary>
    private int CreateNewExtend(byte colID, CollectionFreePages freePages)
    {
        //TODO: lock, pois não pode ter 2 threads aqui


        // try create extend in all AM pages already exists
        foreach (var page in _pages)
        {
            // create new extend on page (if this page contains empty extends)
            var created = page.CreateNewExtend(colID, freePages);

            if (created)
            {
                // return first empty page
                return freePages.EmptyPages.Dequeue();
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
        newPage.CreateNewExtend(colID, freePages);

        // return first empty page
        return freePages.EmptyPages.Dequeue();
    }

    /// <summary>
    /// Update all map position pages based on 
    /// </summary>
    public void UpdateMap(IEnumerable<PageBuffer> modifiedPages)
    {
        // nesse processo deve atualizar _collectionFreePages, adicionando as paginas no lugar certo
        // (não pode pre-existir, pois já foi "dequeue")
        // tenho que cuidar a situação de paginas que ficam mudam o tipo para 

        // deve atualizar também o buffer das AM pages envolvidas.
        // Não há criação de AM pages aqui

        foreach(var page in modifiedPages)
        {
            var pageID = page.Header.PageID;
            var allocationMapID = (pageID / AM_PAGE_STEP);
            var extendIndex = (pageID - 1 - allocationMapID * AM_PAGE_STEP) / AM_EXTEND_SIZE;
            var pageIndex = (pageID - 1 - allocationMapID * AM_PAGE_STEP - extendIndex * AM_EXTEND_SIZE);
            byte value = (page.Header.PageType, page.Header.FreeBytes) switch
            {
                (_, PAGE_CONTENT_SIZE) => 0, // empty page, no matter page type
                (PageType.Data, >= AM_DATA_PAGE_SPACE_LARGE and < PAGE_CONTENT_SIZE) => 1,
                (PageType.Data, >= AM_DATA_PAGE_SPACE_MEDIUM) => 2,
                (PageType.Data, >= AM_DATA_PAGE_SPACE_SMALL) => 3,
                (PageType.Data, < AM_DATA_PAGE_SPACE_SMALL) => 4,
                (PageType.Index, >= AM_INDEX_PAGE_SPACE) => 5,
                (PageType.Index, < AM_INDEX_PAGE_SPACE) => 6,
                _ => throw new NotSupportedException()
            };

            ENSURE(pageIndex != -1, "PageID cannot be an AM page ID");

            var mapPage = _pages[allocationMapID];

            // update buffer map
            mapPage.UpdateMap(extendIndex, pageIndex, value);

            // get (or create) collection free page for this collection
            var freePages = _collectionFreePages[page.Header.ColID] = 
                _collectionFreePages[page.Header.ColID] ?? new CollectionFreePages();

            switch (value)
            {
                case 0b000: // 0
                    freePages.EmptyPages.Insert(pageID);
                    break;
                case 0b001: // 1
                    freePages.DataPagesLarge.Insert(pageID);
                    break;
                case 0b010: // 2
                    freePages.DataPagesMedium.Insert(pageID);
                    break;
                case 0b011: // 3
                    freePages.DataPagesSmall.Insert(pageID);
                    break;
                case 0b100: // 4 - data page full
                    break;
                case 0b101: // 5 - index page with available space
                    freePages.IndexPages.Insert(pageID);
                    break;
                case 0b110: // 6 - index page full
                case 0b111: // 7 - reserved
                    break;

            }
        }
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