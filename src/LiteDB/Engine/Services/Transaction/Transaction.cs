namespace LiteDB.Engine;

/// <summary>
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class Transaction : ITransaction
{
    // dependency injection
    private readonly I__DiskService _diskService;
    private readonly ILogService _logService;
    private readonly IWalIndexService _walIndexService;
    private readonly IAllocationMapService _allocationMapService;
    private readonly I__IndexPageService _indexPageService;
    private readonly I__DataPageService _dataPageService;
    private readonly IBufferFactory _bufferFactory;
    private readonly I__CacheService _cacheService;
    private readonly ILockService _lockService;

    // count how many locks this transaction contains
    private int _lockCounter = 0;

    // rented reader stream
    private I__DiskStream? _reader;

    // local page cache - contains only data/index pages about this collection
    private readonly Dictionary<int, PageBuffer> _localPages = new();

    // local index cache nodes
    private readonly Dictionary<PageAddress, __IndexNode> _localIndexNodes = new();

    // when safepoint occurs, save reference for changed pages on log (PageID, PositionID)
    private readonly Dictionary<int, int> _walDirtyPages = new();

    // original extend values from all requested writable pages
    private readonly Dictionary<int, uint> _initialExtendValues = new();

    // all writable collections ID (must be lock on init)
    private readonly byte[] _writeCollections;

    // for each writeCollection, a cursor for current extend disk position (for data/index per collection)
    private readonly ExtendLocation[] _currentIndexExtend;
    private readonly ExtendLocation[] _currentDataExtend;

    /// <summary>
    /// Read wal version
    /// </summary>
    public int ReadVersion { get; private set; }

    /// <summary>
    /// Incremental transaction ID
    /// </summary>
    public int TransactionID { get; }

    /// <summary>
    /// Get how many pages, in memory, this transaction are using
    /// </summary>
    public int PagesUsed => _localPages.Count;

    public Transaction(
        I__DiskService diskService,
        ILogService logService,
        IBufferFactory bufferFactory,
        I__CacheService cacheService,
        IWalIndexService walIndexService,
        IAllocationMapService allocationMapService,
        I__IndexPageService indexPageService,
        I__DataPageService dataPageService,
        ILockService lockService,
        int transactionID, byte[] writeCollections, int readVersion)
    {
        _diskService = diskService;
        _logService = logService;
        _bufferFactory = bufferFactory;
        _cacheService = cacheService;
        _walIndexService = walIndexService;
        _allocationMapService = allocationMapService;
        _indexPageService = indexPageService;
        _dataPageService = dataPageService;
        _lockService = lockService;

        this.TransactionID = transactionID;
        this.ReadVersion = readVersion; // -1 means not initialized

        _writeCollections = writeCollections;
        _currentIndexExtend = new ExtendLocation[writeCollections.Length];
        _currentDataExtend = new ExtendLocation[writeCollections.Length];
    }

    /// <summary>
    /// Initialize transaction enter in database read lock
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        // enter transaction lock
        await _lockService.EnterTransactionAsync();

        for(var i = 0; i < _writeCollections.Length; i++)
        {
            // enter in all
            await _lockService.EnterCollectionWriteLockAsync(_writeCollections[i]);

            // increment lockCounter to dispose control
            _lockCounter++;
        }

        // if readVersion is -1 must be initialized with next read version from wal
        if (this.ReadVersion == -1)
        {
            // initialize read version from wal
            this.ReadVersion = _walIndexService.GetNextReadVersion();
        }

        ENSURE(this.ReadVersion >= _walIndexService.MinReadVersion, $"Read version do not exists in wal index: {this.ReadVersion} >= {_walIndexService.MinReadVersion}", new { self = this });
    }

    /// <summary>
    /// Get a existing page on database based on ReadVersion. Try get first from localPages,
    /// cache and in last case read from disk (and add to localPages)
    /// </summary>
    public async ValueTask<PageBuffer> GetPageAsync(int pageID)
    {
        using var _pc = PERF_COUNTER(8, nameof(GetPageAsync), nameof(Transaction));

        ENSURE(pageID != int.MaxValue, "PageID must have a value");

        if (_localPages.TryGetValue(pageID, out var page))
        {
            // if writable, page should not be in cache
            ENSURE(Array.IndexOf(_writeCollections, page.Header.ColID) > -1, page.ShareCounter == NO_CACHE, "Page should not be in cache", new { _writeCollections, page });

            return page;
        }

        page = await this.ReadPageAsync(pageID, this.ReadVersion);

        _localPages.Add(pageID, page);

        return page;
    }

    /// <summary>
    /// Read a data/index page from disk (data or log). Can return page from global cache
    /// </summary>
    private async ValueTask<PageBuffer> ReadPageAsync(int pageID, int readVersion)
    {
        using var _pc = PERF_COUNTER(9, nameof(ReadPageAsync), nameof(Transaction));

        _reader ??= _diskService.RentDiskReader();

        // test if page are in transaction wal pages
        if (_walDirtyPages.TryGetValue(pageID, out var positionID))
        {
            var walPage = _bufferFactory.AllocateNewPage();

            await _reader.ReadPageAsync(positionID, walPage);

            ENSURE(walPage.Header.PageType == PageType.Data || walPage.Header.PageType == PageType.Index, $"Only data/index page on transaction read page: {walPage}", new { walPage });

            return walPage;
        }

        // get disk position (data/log)
        positionID = _walIndexService.GetPagePositionID(pageID, readVersion, out _);

        // get a page from cache (if writable, this page are not linked to cache anymore)
        var page = _cacheService.GetPageReadWrite(positionID, _writeCollections, out var writable);

        // if page not found, allocate new page and read from disk
        if (page is null)
        {
            page = _bufferFactory.AllocateNewPage();

            await _reader.ReadPageAsync(positionID, page);

            ENSURE(page.Header.PageType == PageType.Data || page.Header.PageType == PageType.Index, $"Only data/index page on transaction read page: {page}");
        }

        return page;
    }

    public __IndexNode GetIndexNode(PageAddress indexNodeID)
    {
        using var _pc = PERF_COUNTER(10, nameof(GetIndexNode), nameof(Transaction));

        if (_localIndexNodes.TryGetValue(indexNodeID, out var indexNode))
        {
            return indexNode;
        }

        _localPages.TryGetValue(indexNodeID.PageID, out var page);

        ENSURE(page is not null, "Page not found for this index", new { indexNodeID });

        indexNode = new __IndexNode(page!, indexNodeID);

        _localIndexNodes.Add(indexNodeID, indexNode);

        return indexNode;
    }

    public void DeleteIndexNode(PageAddress indexNodeID)
    {
        var deleted = _localIndexNodes.Remove(indexNodeID);

        ENSURE(deleted, "__IndexNode not found in transaction local index cache", new { indexNodeID, _localIndexNodes });
    }

    /// <summary>
    /// Get a Data Page with, at least, 30% free space
    /// </summary>
    public async ValueTask<PageBuffer> GetFreeDataPageAsync(byte colID)
    {
        using var _pc = PERF_COUNTER(11, nameof(GetFreeDataPageAsync), nameof(Transaction));

        var colIndex = Array.IndexOf(_writeCollections, colID);
        var currentExtend = _currentDataExtend[colIndex];

        // request for allocation map service a new PageID for this collection
        var (pageID, isNew, nextExtend) = _allocationMapService.GetFreeExtend(currentExtend, colID, PageType.Data);

        // update current collection extend location
        _currentDataExtend[colIndex] = nextExtend;

        if (isNew)
        {
            var page = _bufferFactory.AllocateNewPage();

            // initialize empty page as data page
            _dataPageService.InitializeDataPage(page, pageID, colID);

            // add in local cache
            _localPages.Add(pageID, page);

            return page;
        }
        else
        {
            // if page already exists, just get page
            var page = await this.GetPageAsync(pageID);

            return page;
        }
    }

    /// <summary>
    /// Get a Index Page with space enougth for index node
    /// </summary>
    public async ValueTask<PageBuffer> GetFreeIndexPageAsync(byte colID, int indexNodeLength)
    {
        using var _pc = PERF_COUNTER(12, nameof(GetFreeIndexPageAsync), nameof(Transaction));

        var colIndex = Array.IndexOf(_writeCollections, colID);
        var currentExtend = _currentIndexExtend[colIndex];

        // request for allocation map service a new PageID for this collection
        var (pageID, isNew, nextExtend) = _allocationMapService.GetFreeExtend(currentExtend, colID, PageType.Index);

        // update current collection extend location
        _currentIndexExtend[colIndex] = nextExtend;

        if (isNew)
        {
            var page = _bufferFactory.AllocateNewPage();

            // initialize empty page as index page
            _indexPageService.InitializeIndexPage(page, pageID, colID);

            // add in local cache
            _localPages.Add(pageID, page);

            return page;
        }
        else
        {
            var page = await this.GetPageAsync(pageID);

            // if current page has no avaiable space (super rare cases), get another page
            if (page.Header.FreeBytes < indexNodeLength)
            {
                // set this page as full before get next page
                this.UpdatePageMap(page.Header.PageID, ExtendPageValue.Full);

                // call recursive to get another page
                return await this.GetFreeIndexPageAsync(colID, indexNodeLength);
            }

            return page;
        }
    }

    /// <summary>
    /// Update allocation page map according with header page type and used bytes but keeps a copy
    /// of original extend value (if need rollback)
    /// </summary>
    public void UpdatePageMap(int pageID, ExtendPageValue value)
    {
        var allocationMapID = (int)(pageID / AM_PAGE_STEP);
        var extendIndex = (pageID - 1 - allocationMapID * AM_PAGE_STEP) / AM_EXTEND_SIZE;

        var extendLocation = new ExtendLocation(allocationMapID, extendIndex);
        var extendID = extendLocation.ExtendID;

        if (!_initialExtendValues.ContainsKey(extendID))
        {
            var extendValue = _allocationMapService.GetExtendValue(extendLocation);

            _initialExtendValues.Add(extendID, extendValue);
        }

        _allocationMapService.UpdatePageMap(pageID, value);
    }

    /// <summary>
    /// Persist current pages changes and discard all local pages. Works as a Commit, but without
    /// marking last page as confirmed
    /// </summary>
    public async Task SafepointAsync()
    {
        // get dirty pages only //TODO: can be re-used array?
        var dirtyPages = _localPages.Values
            .Where(x => x.IsDirty)
            .ToArray();

        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = dirtyPages[i];

            ENSURE(page.ShareCounter == NO_CACHE, "Page should not be on cache when saving", page);
            
            // update page header
            page.Header.TransactionID = this.TransactionID;
            page.Header.IsConfirmed = false;
        }

        // write pages on disk and flush data
        await _logService.WriteLogPagesAsync(dirtyPages);

        // update local transaction wal index
        foreach (var page in dirtyPages)
        {
            _walDirtyPages[page.Header.PageID] = page.PositionID;
        }

        // add pages to cache or decrement sharecount
        foreach (var page in _localPages.Values)
        {
            if (page.ShareCounter > 0)
            {
                // page already in cache (was not changed)
                _cacheService.ReturnPageToCache(page);
            }
            else
            {
                // all other pages are not came from cache, must be deallocated
                _bufferFactory.DeallocatePage(page);
            }
        }

        // clear page buffer references
        _localPages.Clear();
        _localIndexNodes.Clear();
    }

    /// <summary>
    /// </summary>
    public async ValueTask CommitAsync()
    {
        using var _pc = PERF_COUNTER(59, nameof(CommitAsync), nameof(Transaction));

        // get dirty pages only //TODO: can be re-used array?
        var dirtyPages = _localPages.Values
            .Where(x => x.IsDirty)
            .ToArray();

        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = dirtyPages[i];

            ENSURE(page.ShareCounter == NO_CACHE, "Page should not be on cache when saving", page);

            // update page header
            page.Header.TransactionID = this.TransactionID;
            page.Header.IsConfirmed = i == (dirtyPages.Length - 1);
        }

        // write pages on disk and flush data
        await _logService.WriteLogPagesAsync(dirtyPages);

        // update wal index with this new version
        IEnumerable<(int pageID, int positionID)> changedPages()
        {
            foreach (var pageRef in _walDirtyPages)
            {
                yield return (pageRef.Key, pageRef.Value);
            }
            foreach (var page in dirtyPages)
            {
                yield return (page.Header.PageID, page.PositionID);
            }
        }

        _walIndexService.AddVersion(this.ReadVersion, changedPages());

        // add pages to cache or decrement sharecount
        foreach (var page in _localPages.Values)
        {
            // page already in cache (was not changed)
            if (page.ShareCounter > 0)
            {
                _cacheService.ReturnPageToCache(page);
            }
            else
            {
                // try add this page in cache
                var added = _cacheService.AddPageInCache(page);

                if (!added)
                {
                    _bufferFactory.DeallocatePage(page);
                }
            }
        }

        // clear page buffer references
        _localPages.Clear();
        _localIndexNodes.Clear();
        _walDirtyPages.Clear();
    }

    public void Rollback()
    {
        using var _pc = PERF_COUNTER(48, nameof(Rollback), nameof(Transaction));

        // add pages to cache or decrement sharecount
        foreach (var page in _localPages.Values)
        {
            if (page.IsDirty)
            {
                _bufferFactory.DeallocatePage(page);
            }
            else
            {
                // test if page is came from the cache
                if (page.ShareCounter > 0)
                {
                    // return page to cache
                    _cacheService.ReturnPageToCache(page);
                }
                else
                {
                    // try add this page in cache
                    var added = _cacheService.AddPageInCache(page);

                    if (!added)
                    {
                        _bufferFactory.DeallocatePage(page);
                    }
                }
            }
        }

        // clear page buffer references
        _localPages.Clear();
        _localIndexNodes.Clear();
        _walDirtyPages.Clear();

        // restore initial values in allocation map to return original state before any change
        if (_initialExtendValues.Count > 0)
        {
            _allocationMapService.RestoreExtendValues(_initialExtendValues);
        }

        _initialExtendValues.Clear();
    }

    public override string ToString()
    {
        return Dump.Object(new { TransactionID, ReadVersion, _localPages, _localIndexNodes, _writeCollections, _initialExtendValues, _currentIndexExtend, _currentDataExtend, _lockCounter });
    }

    public void Dispose()
    {
        // return reader if used
        if (_reader is not null)
        {
            _diskService.ReturnDiskReader(_reader);
        }

        // Transaction = 0 means is created for first $master read. There are an ExclusiveLock befre
        if (this.TransactionID == 0) return;

        while (_lockCounter > 0)
        {
            _lockService.ExitCollectionWriteLock(_writeCollections[_lockCounter - 1]);
            _lockCounter--;
        }

        // exit lock transaction
        _lockService.ExitTransaction();

        ENSURE(_localPages.Count == 0, $"Missing dispose pages in transaction", new { _localPages });
        ENSURE(_lockCounter == 0, $"Missing release lock in transaction", new { _localPages, _lockCounter });
    }
}