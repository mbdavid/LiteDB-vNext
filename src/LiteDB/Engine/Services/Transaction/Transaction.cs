﻿namespace LiteDB.Engine;

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
    private readonly IIndexPageService _indexPageService;
    private readonly IDataPageService _dataPageService;
    private readonly IBufferFactory _bufferFactory;
    private readonly ICacheService _cacheService;
    private readonly ILockService _lockService;

    // count how many locks this transaction contains
    private int _lockCounter = 0;

    // rented reader stream
    private IDiskStream? _reader;

    // local page cache - contains only data/index pages about this collection
    private readonly Dictionary<int, PageBuffer> _localPages = new();

    // when safepoint occurs, save reference for changed pages on log (PageID, PositionID)
    private readonly Dictionary<int, int> _walDirtyPages = new();

    // original extend values from all requested writable pages
    private readonly Dictionary<int, uint> _initialExtendValues = new();

    // all writable collections ID (must be lock on init)
    private readonly byte[] _writeCollections;

    // for each writeCollection, a cursor for current extend disk position
    private readonly ExtendLocation[] _currentExtendLocations;

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
        IBufferFactory bufferFactory,
        ICacheService cacheService,
        IWalIndexService walIndexService,
        IAllocationMapService allocationMapService,
        IIndexPageService indexPageService,
        IDataPageService dataPageService,
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
        _currentExtendLocations = new ExtendLocation[writeCollections.Length];
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

        ENSURE(() => this.ReadVersion >= _walIndexService.MinReadVersion, $"Read version do not exists in wal index: {this.ReadVersion} >= {_walIndexService.MinReadVersion}");
    }

    /// <summary>
    /// Get a existing page on database based on ReadVersion. Try get first from localPages,
    /// cache and in last case read from disk (and add to localPages)
    /// </summary>
    public async ValueTask<PageBuffer> GetPageAsync(int pageID)
    {
        ENSURE(() => pageID != int.MaxValue, "PageID must have a value");

        if (_localPages.TryGetValue(pageID, out var page))
        {
            // if writable, page should not be in cache
            ENSURE(Array.IndexOf(_writeCollections, page.Header.ColID) > -1, () => page.ShareCounter == NO_CACHE, "Page should not be in cache");

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
        _reader ??= _diskService.RentDiskReader();

        // test if page are in transaction wal pages
        if (_walDirtyPages.TryGetValue(pageID, out var positionID))
        {
            var walPage = _bufferFactory.AllocateNewPage(false);

            await _reader.ReadPageAsync(positionID, walPage);

            ENSURE(() => walPage.Header.PageType == PageType.Data || walPage.Header.PageType == PageType.Index, $"Only data/index page on transaction read page: {walPage}");

            return walPage;
        }

        // get disk position (data/log)
        positionID = _walIndexService.GetPagePositionID(pageID, readVersion, out _);

        // get a page from cache (if writable, this page are not linked to cache anymore)
        var page = _cacheService.GetPageReadWrite(positionID, _writeCollections, out var writable);

        // if page not found, allocate new page and read from disk
        if (page is null)
        {
            page = _bufferFactory.AllocateNewPage(false);

            await _reader.ReadPageAsync(positionID, page);

            ENSURE(() => page.Header.PageType == PageType.Data || page.Header.PageType == PageType.Index, $"Only data/index page on transaction read page: {page}");
        }

        return page;
    }

    /// <summary>
    /// Get a Data Page with, at least, 30% free space
    /// </summary>
    public async ValueTask<PageBuffer> GetFreeDataPageAsync(byte colID)
    {
        var colIndex = Array.IndexOf(_writeCollections, colID);
        var currentExtend = _currentExtendLocations[colIndex];

        // request for allocation map service a new PageID for this collection
        var (pageID, isNew, nextExtend) = _allocationMapService.GetFreeExtend(currentExtend, colID, PageType.Data);

        // update current collection extend location
        _currentExtendLocations[colIndex] = nextExtend;

        if (isNew)
        {
            var page = _bufferFactory.AllocateNewPage(true);

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
        var colIndex = Array.IndexOf(_writeCollections, colID);
        var currentExtend = _currentExtendLocations[colIndex];

        // request for allocation map service a new PageID for this collection
        var (pageID, isNew, nextExtend) = _allocationMapService.GetFreeExtend(currentExtend, colID, PageType.Index);

        // update current collection extend location
        _currentExtendLocations[colIndex] = nextExtend;

        if (isNew)
        {
            var page = _bufferFactory.AllocateNewPage(true);

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

            ENSURE(() => page.ShareCounter == NO_CACHE, "Page should not be on cache when saving");
            ENSURE(() => page.Header.IsConfirmed == false);

            // update page header
            page.Header.TransactionID = this.TransactionID;
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
                _cacheService.ReturnPage(page);
            }
            else
            {
                // all other pages are not came from cache, must be deallocated
                _bufferFactory.DeallocatePage(page);
            }
        }

        // clear page buffer references
        _localPages.Clear();
    }

    /// <summary>
    /// </summary>
    public async ValueTask CommitAsync()
    {
        // get dirty pages only //TODO: can be re-used array?
        var dirtyPages = _localPages.Values
            .Where(x => x.IsDirty)
            .ToArray();

        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = dirtyPages[i];

            ENSURE(() => page.ShareCounter == NO_CACHE, "Page should not be on cache when saving");

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
                _cacheService.ReturnPage(page);
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
        _walDirtyPages.Clear();
    }

    public void Rollback()
    {
        // add pages to cache or decrement sharecount
        foreach (var page in _localPages.Values)
        {
            if (page.IsDirty/* || page.Header.ColID == MASTER_COL_ID*/)
            {
                _bufferFactory.DeallocatePage(page);
            }
            else
            {
                // test if page is came from the cache
                if (page.ShareCounter > 0)
                {
                    // return page to cache
                    _cacheService.ReturnPage(page);
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

        // restore initial values in allocation map to return original state before any change
        if (_initialExtendValues.Count > 0)
        {
            _allocationMapService.RestoreExtendValues(_initialExtendValues);
        }

        _initialExtendValues.Clear();
    }

    public void Dispose()
    {
        // return reader if used
        if (_reader is not null)
        {
            _diskService.ReturnDiskReader(_reader);
        }

        while (_lockCounter > 0)
        {
            _lockService.ExitCollectionWriteLock(_writeCollections[_lockCounter - 1]);
            _lockCounter--;
        }

        // exit lock transaction
        _lockService.ExitTransaction();

        ENSURE(() => _localPages.Count == 0, $"Missing dispose pages in transaction");
        ENSURE(() =>_lockCounter == 0, $"Missing release lock in transaction");
    }
}