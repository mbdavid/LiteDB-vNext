namespace LiteDB.Engine;

/// <summary>
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class Transaction : ITransaction
{
    // dependency injection
    private readonly IServicesFactory _factory;
    private readonly IDiskService _disk;
    private readonly IWalIndexService _walIndex;
    private readonly IAllocationMapService _allocationMap;
    private readonly IDataPageService _dataPage;
    private readonly IBufferFactory _bufferFactory;
    private readonly IMemoryCacheService _memoryCache;
    private readonly ILockService _lock;

    private IDiskStream? _reader;

    // local page cache - contains only data/index pages about this collection
    private readonly IDictionary<uint, PageBuffer> _localPages = new Dictionary<uint, PageBuffer>();

    //
    private readonly byte[] _writeCollections;

    public int TransactionID { get; }

    public Transaction(IServicesFactory factory, int transactionID, byte[] writeCollections)
    {
        _factory = factory;
        _disk = factory.GetDisk();
        _bufferFactory = factory.GetBufferFactory();
        _memoryCache = factory.GetMemoryCache();
        _walIndex = factory.GetWalIndex();
        _allocationMap = factory.GetAllocationMap();
        _dataPage = factory.GetDataPageService();
        _lock = factory.GetLock();

        this.TransactionID = transactionID;
        _writeCollections = writeCollections;
    }

    /// <summary>
    /// Initialize transaction enter in database read lock
    /// </summary>
    public async Task InitializeAsync()
    {
        // enter transaction lock
        await _lock.EnterTransactionAsync();

        for(var i = 0; i < _writeCollections.Length; i++)
        {
            // enter in all
            await _lock.EnterCollectionWriteLockAsync(_writeCollections[i]);
        }

    }

    /// <summary>
    /// Try get page from local cache. If page not found, use ReadPage from disk
    /// </summary>
    public async Task<PageBuffer> GetPageAsync(uint pageID, bool writable)
    {
        if (_localPages.TryGetValue(pageID, out var page))
        {
            ENSURE(writable, page.ShareCounter == 0, "page should not be in cache");

            return page;
        }

        page = await this.ReadPageAsync(pageID, this.TransactionID, writable); // transactionID = current ReadVersion

        _localPages.Add(pageID, page);

        return page;
    }

    /// <summary>
    /// Read a page from disk. Use direct position (data) or index log
    /// </summary>
    private async Task<PageBuffer> ReadPageAsync(uint pageID, int readVersion, bool writable)
    {
        _reader ??= await _disk.RentDiskReaderAsync();

        // get disk position (data/log)
        var position = _walIndex.GetPagePosition(pageID, readVersion, out _);

        // test if available in cache
        var page = _memoryCache.GetPage(position);

        // if page not found, allocate new page and read from disk
        if (page is null)
        {
            page = _bufferFactory.AllocateNewPage(writable);

            await _reader.ReadPageAsync(position, page);
        }
        // if found in cache but need to be writable
        else if (writable)
        {
            // if readBuffer are not used by anyone in cache (ShareCounter == 1 - only current thread), remove it
            if (_memoryCache.TryRemovePageFromCache(page, 1))
            {
                page.IsDirty = true;

                // it's safe here to use readBuffer as writeBuffer (nobody else are reading)
                return page;
            }
            else
            {
                // create a new page in memory
                var newPage = _bufferFactory.AllocateNewPage(true);

                // copy cache page content to new writable buffer
                page.CopyBufferTo(newPage);

                return newPage;
            }
        }

        return page;
    }

    /// <summary>
    /// Get a page with free space avaiable to store, at least, bytesLength
    /// </summary>
    public async Task<PageBuffer> GetFreePageAsync(byte colID, PageType pageType, int bytesLength)
    {
        // first check if exists in localPages (TODO: como indexar isso??)
        var localPage = _localPages.Values
            .Where(x => x.Header.PageType == pageType && x.Header.FreeBytes >= bytesLength)
            .FirstOrDefault();

        if (localPage is not null) return localPage;

        // request for allocation map service a new PageID for this collection
        var (pageID, isNew) = _allocationMap.GetFreePageID(colID, PageType.Data, bytesLength);

        if (isNew)
        {
            var page = _bufferFactory.AllocateNewPage(true);

            _dataPage.CreateNew(page, pageID, colID);

            _localPages.Add(pageID, page);

            return page;
        }
        else
        {
            var page = await this.GetPageAsync(pageID, true);

            return page;
        }
    }


    /// <summary>
    /// Add a new page recently created page to local cache
    /// </summary>
    public void AddPage(PageBuffer page)
    {
        _localPages.Add(page.Header.PageID, page);

    }

    /// <summary>
    /// </summary>
    public async Task CommitAsync()
    {
        var dirtyPages = _localPages.Values
            .Where(x => x.IsDirty)
            .ToArray();

        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = dirtyPages[i];

            ENSURE(page.ShareCounter == 0, "page should not be on cache when saving");

            page.Position = _allocationMap.GetNextLogPosition(page.Header.PageID);

            page.Header.TransactionID = this.TransactionID;
            page.Header.IsConfirmed = i == (dirtyPages.Length - 1);
        }

        // write pages on disk and flush data
        await _disk.WritePagesAsync(dirtyPages);

        // update allocation map with all dirty pages
        _allocationMap.UpdateMap(dirtyPages);

        // add pages to cache or decrement sharecount
        foreach(var page in _localPages.Values)
        {
            if (page.ShareCounter > 0)
            {
                page.Return();
            }
            else
            {
                var added = _memoryCache.AddPageInCache(page);

                if (!added)
                {
                    _bufferFactory.DeallocatePage(page);
                }
            }

        }

        var pagePositions = _localPages.Values
            .Select(x => (x.Header.PageID, x.Position));

        _walIndex.AddVersion(this.TransactionID, pagePositions);

    }

    public void Rollback()
    {
        
    }

    public void Dispose()
    {
        for (var i = 0; i < _writeCollections.Length; i++)
        {
            // exit in all collection locks
            _lock.ExitCollectionWriteLock(_writeCollections[i]);
        }

        // exit lock transaction
        _lock.ExitTransaction();
    }
}