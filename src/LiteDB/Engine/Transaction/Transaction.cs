namespace LiteDB.Engine;

/// <summary>
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class Transaction : ITransaction
{
    // dependency injection
    private readonly IDiskService _diskService;
    private readonly ILogService _logService;
    private readonly IWalIndexService _walIndexService;
    private readonly IAllocationMapService _allocationMapService;
    private readonly IMemoryFactory _memoryFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly ILockService _lockService;

    // count how many locks this transaction contains
    private int _lockCounter = 0;

    // rented reader stream
    private IDiskStream? _reader;

    // local page cache - contains only data/index pages about this collection
    private readonly Dictionary<uint, nint> _localPages = new();

    // when safepoint occurs, save reference for changed pages on log (PageID, PositionID)
    private readonly Dictionary<uint, uint> _walDirtyPages = new();

    // original extend values from all requested writable pages (ExtendID, ExtendValue)
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
        IDiskService diskService,
        ILogService logService,
        IMemoryFactory memoryFactory,
        IMemoryCache memoryCache,
        IWalIndexService walIndexService,
        IAllocationMapService allocationMapService,
        ILockService lockService,
        int transactionID, byte[] writeCollections, int readVersion)
    {
        _diskService = diskService;
        _logService = logService;
        _memoryFactory = memoryFactory;
        _memoryCache = memoryCache;
        _walIndexService = walIndexService;
        _allocationMapService = allocationMapService;
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
    public unsafe PageMemory* GetPage(uint pageID)
    {
        using var _pc = PERF_COUNTER(90, nameof(GetPage), nameof(Transaction));

        ENSURE(pageID != uint.MaxValue, "PageID must have a value");

        if (_localPages.TryGetValue(pageID, out var ptr))
        {
            var page = (PageMemory*)ptr;

            // if writable, page should not be in cache
            ENSURE(Array.IndexOf(_writeCollections, page->ColID) > -1, page->ShareCounter == NO_CACHE, "Page should not be in cache", new { _writeCollections });

            return page;
        }

        var newPage = this.ReadPage(pageID, this.ReadVersion);

        _localPages.Add(pageID, (nint)newPage);

        return newPage;
    }

    /// <summary>
    /// Read a data/index page from disk (data or log). Can return page from global cache
    /// </summary>
    private unsafe PageMemory* ReadPage(uint pageID, int readVersion)
    {
        using var _pc = PERF_COUNTER(100, nameof(ReadPage), nameof(Transaction));

        _reader ??= _diskService.RentDiskReader();

        var writable = false;
        var found = false;

        // test if page are in local wal pages
        if (_walDirtyPages.TryGetValue(pageID, out var positionID))
        {
            // if page are in local wal, try get from cache
            var cachePage = _memoryCache.GetPageReadWrite(positionID, _writeCollections, out writable, out found);

            if (found == false)
            {
                // if not found, allocate new page
                var walPage = _memoryFactory.AllocateNewPage();

                _reader.ReadPage(walPage, positionID);

                ENSURE(walPage->PageType == PageType.Data || walPage->PageType == PageType.Index, $"Only data/index page on transaction read page: {walPage->PageID}");

                return walPage;
            }
            else
            {
                return cachePage;
            }
        }

        // get disk position from global wal (data/log)
        positionID = _walIndexService.GetPagePositionID(pageID, readVersion, out _);

        // get a page from cache (if writable, this page are not linked to cache anymore)
        var page = _memoryCache.GetPageReadWrite(positionID, _writeCollections, out writable, out found);

        // if page not found, allocate new page and read from disk
        if (found == false)
        {
            page = _memoryFactory.AllocateNewPage();

            _reader.ReadPage(page, positionID);

            ENSURE(page->PageType == PageType.Data || page->PageType == PageType.Index, $"Only data/index page on transaction read page: {page->PageID}");
        }

        return page;
    }

    /// <summary>
    /// Get a Data Page with, at least, 30% free space
    /// </summary>
    public unsafe PageMemory* GetFreeDataPage(byte colID)
    {
        using var _pc = PERF_COUNTER(110, nameof(GetFreeDataPage), nameof(Transaction));

        var colIndex = Array.IndexOf(_writeCollections, colID);
        var currentExtend = _currentDataExtend[colIndex];

        // request for allocation map service a new PageID for this collection
        var (pageID, isNew, nextExtend) = _allocationMapService.GetFreeExtend(currentExtend, colID, PageType.Data);

        // update current collection extend location
        _currentDataExtend[colIndex] = nextExtend;

        if (isNew)
        {
            var page = _memoryFactory.AllocateNewPage();

            // initialize empty page as data page
            PageMemory.InitializeAsDataPage(page, pageID, colID);

            // add in local cache
            _localPages.Add(pageID, (nint)page);

            return page;
        }
        else
        {
            // if page already exists, just get page
            var page = this.GetPage(pageID);

            return page;
        }
    }

    /// <summary>
    /// Get a Index Page with space enougth for index node
    /// </summary>
    public unsafe PageMemory* GetFreeIndexPage(byte colID, int indexNodeLength)
    {
        using var _pc = PERF_COUNTER(120, nameof(GetFreeIndexPage), nameof(Transaction));

        var colIndex = Array.IndexOf(_writeCollections, colID);
        var currentExtend = _currentIndexExtend[colIndex];

        // request for allocation map service a new PageID for this collection
        var (pageID, isNew, nextExtend) = _allocationMapService.GetFreeExtend(currentExtend, colID, PageType.Index);

        // update current collection extend location
        _currentIndexExtend[colIndex] = nextExtend;

        if (isNew)
        {
            var page = _memoryFactory.AllocateNewPage();

            // initialize empty page as index page
            PageMemory.InitializeAsIndexPage(page, pageID, colID);

            // add in local cache
            _localPages.Add(pageID, (nint)page);

            return page;
        }
        else
        {
            var page = this.GetPage(pageID);

            // if current page has no avaiable space (super rare cases), get another page
            if (page->FreeBytes < indexNodeLength)
            {
                // set this page as full before get next page
                this.UpdatePageMap(page->PageID, ExtendPageValue.Full);

                // call recursive to get another page
                return this.GetFreeIndexPage(colID, indexNodeLength);
            }

            return page;
        }
    }

    /// <summary>
    /// Update allocation page map according with header page type and used bytes but keeps a copy
    /// of original extend value (if need rollback)
    /// </summary>
    public void UpdatePageMap(uint pageID, ExtendPageValue value)
    {
        var allocationMapID = (int)(pageID / AM_PAGE_STEP);
        var extendIndex = (pageID - 1 - allocationMapID * AM_PAGE_STEP) / AM_EXTEND_SIZE;

        var extendLocation = new ExtendLocation(allocationMapID, (int)extendIndex);
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
        using var _pc = PERF_COUNTER(130, nameof(SafepointAsync), nameof(Transaction));

        this.SafepointInternal();

        await _diskService.GetDiskWriter().FlushAsync();
    }

    private unsafe void SafepointInternal()
    {
        // get dirty pages only //TODO: can be re-used array?
        var dirtyPages = _localPages.Values
            .Where(x => ((PageMemory*)x)->IsDirty)
            .ToArray();

        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = (PageMemory*)dirtyPages[i];

            ENSURE(page->ShareCounter == NO_CACHE, "Page should not be on cache when saving");

            // update page header
            page->TransactionID = this.TransactionID;
            page->IsConfirmed = false;
        }

        // write pages on disk and flush data (updates PositionID)
        _logService.WriteLogPages(dirtyPages);

        // update local transaction wal index
        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = (PageMemory*)dirtyPages[i];

            _walDirtyPages[page->PageID] = page->PositionID;
        }

        // add pages to cache or decrement sharecount
        foreach (var ptr in _localPages.Values)
        {
            var page = (PageMemory*)ptr;

            if (page->IsPageInCache)
            {
                // page already in cache (was not changed)
                _memoryCache.ReturnPageToCache(page);
            }
            else
            {
                // add page to cache (even if is on "non-commited" transaction)
                // only this transactions knows about this new positionIDs
                var added = _memoryCache.AddPageInCache(page);

                if (!added)
                {
                    _memoryFactory.DeallocatePage(page);
                }
            }
        }

        // clear page buffer references
        _localPages.Clear();
    }

    /// <summary>
    /// </summary>
    public async ValueTask CommitAsync()
    {
        using var _pc = PERF_COUNTER(140, nameof(CommitAsync), nameof(Transaction));

        this.CommitInternal();

        await _diskService.GetDiskWriter().FlushAsync();
    }

    private unsafe void CommitInternal()
    {
        // get dirty pages only //TODO: can be re-used array?
        var dirtyPages = _localPages.Values
            .Where(x => ((PageMemory*)x)->IsDirty)
            .ToArray();

        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = (PageMemory*)dirtyPages[i];

            ENSURE(page->ShareCounter == NO_CACHE, "Page should not be on cache when saving");

            // update page header
            page->TransactionID = this.TransactionID;
            page->IsConfirmed = i == (dirtyPages.Length - 1);
        }

        // write pages on disk and flush data (updates PositionID)
        _logService.WriteLogPages(dirtyPages);

        // update wal index with this new version
        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = (PageMemory*)dirtyPages[i];

            _walDirtyPages[page->PageID] = page->PositionID;
        }

        // update wal index with new page set 
        _walIndexService.AddVersion(this.ReadVersion, _walDirtyPages.Select(x => (x.Key, x.Value)));

        // add pages to cache or decrement sharecount
        foreach (var ptr in _localPages.Values)
        {
            var page = (PageMemory*)(ptr);

            // page already in cache (was not changed)
            if (page->IsPageInCache)
            {
                _memoryCache.ReturnPageToCache(page);
            }
            else
            {
                // try add this page in cache
                var added = _memoryCache.AddPageInCache(page);

                if (!added)
                {
                    _memoryFactory.DeallocatePage(page);
                }
            }
        }

        // clear page buffer references
        _localPages.Clear();
        _walDirtyPages.Clear();
    }

    public unsafe void Abort()
    {
        using var _pc = PERF_COUNTER(150, nameof(Abort), nameof(Transaction));

        // add pages to cache or decrement sharecount
        foreach (var ptr in _localPages.Values)
        {
            var page = (PageMemory*)ptr;

            if (page->IsDirty)
            {
                _memoryFactory.DeallocatePage(page);
            }
            else
            {
                // test if page is came from the cache
                if (page->IsPageInCache)
                {
                    // return page to cache
                    _memoryCache.ReturnPageToCache(page);
                }
                else
                {
                    // try add this page in cache
                    var added = _memoryCache.AddPageInCache(page);

                    if (!added)
                    {
                        _memoryFactory.DeallocatePage(page);
                    }
                }
            }
        }

        // clear page buffer references
        _localPages.Clear();
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
        return Dump.Object(new { TransactionID, ReadVersion, _localPages, _writeCollections, _initialExtendValues, _currentIndexExtend, _currentDataExtend, _lockCounter });
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