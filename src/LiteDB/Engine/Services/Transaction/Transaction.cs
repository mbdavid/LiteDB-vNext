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
    private readonly IIndexPageService _indexPage;
    private readonly IDataPageService _dataPage;
    private readonly IBufferFactory _bufferFactory;
    private readonly IMemoryCacheService _memoryCache;
    private readonly ILockService _lock;

    // count how many locks this transaction contains
    private int _lockCounter = 0;

    private IDiskStream? _reader;

    // local page cache - contains only data/index pages about this collection
    private readonly IDictionary<uint, PageBuffer> _localPages = new Dictionary<uint, PageBuffer>();

    //
    private readonly byte[] _writeCollections;

    /// <summary>
    /// Read wal version
    /// </summary>
    public int ReadVersion { get; private set; }

    /// <summary>
    /// Incremental transaction ID
    /// </summary>
    public int TransactionID { get; }

    public Transaction(IServicesFactory factory, int transactionID, byte[] writeCollections, int readVersion)
    {
        _factory = factory;
        _disk = factory.GetDisk();
        _bufferFactory = factory.GetBufferFactory();
        _memoryCache = factory.GetMemoryCache();
        _walIndex = factory.GetWalIndex();
        _allocationMap = factory.GetAllocationMap();
        _indexPage = factory.GetIndexPageService();
        _dataPage = factory.GetDataPageService();
        _lock = factory.GetLock();

        this.TransactionID = transactionID;
        this.ReadVersion = readVersion; // -1 means not initialized

        _writeCollections = writeCollections;
    }

    /// <summary>
    /// Initialize transaction enter in database read lock
    /// </summary>
    public async Task InitializeAsync()
    {
        // enter transaction lock
        await _lock.EnterTransactionAsync();

        _lockCounter = 1;

        for(var i = 0; i < _writeCollections.Length; i++)
        {
            // enter in all
            await _lock.EnterCollectionWriteLockAsync(_writeCollections[i]);

            // increment lockCounter to dispose control
            _lockCounter++;
        }

        // if readVersion is -1 must be initialized with next read version from wal
        if (this.ReadVersion == -1)
        {
            // initialize read version from wal
            this.ReadVersion = _walIndex.GetNextReadVersion();
        }

        ENSURE(this.ReadVersion >= _walIndex.MinReadVersion, $"read version do not exists in wal index: {this.ReadVersion} >= {_walIndex.MinReadVersion}");
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

        page = await this.ReadPageAsync(pageID, this.ReadVersion, writable);

        _localPages.Add(pageID, page);

        return page;
    }

    /// <summary>
    /// Read a data/index page from disk (data or log). Can return page from global cache
    /// </summary>
    private async Task<PageBuffer> ReadPageAsync(uint pageID, int readVersion, bool writable)
    {
        _reader ??= await _disk.RentDiskReaderAsync();

        // get disk position (data/log)
        var positionID = _walIndex.GetPagePositionID(pageID, readVersion, out _);

        // test if available in cache
        var page = _memoryCache.GetPage(positionID);

        // if page not found, allocate new page and read from disk
        if (page is null)
        {
            page = _bufferFactory.AllocateNewPage(writable);

            await _reader.ReadPageAsync(positionID, page);
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
        var (pageID, isNew) = _allocationMap.GetFreePageID(colID, pageType, bytesLength);

        if (isNew)
        {
            var page = _bufferFactory.AllocateNewPage(true);

            // initialize empty page as data/index page
            if (pageType == PageType.Data)
            {
                _dataPage.InitializeDataPage(page, pageID, colID);
            }
            else if (pageType == PageType.Index)
            {
                _indexPage.InitializeIndexPage(page, pageID, colID);
            }
            else throw new NotSupportedException();

            // add in local cache
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
    /// </summary>
    public async Task CommitAsync()
    {
        var dirtyPages = _localPages.Values
            //.Where(x => x.IsDirty)
            .ToArray();

        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = dirtyPages[i];

            ENSURE(page.ShareCounter == PAGE_NO_CACHE, "page should not be on cache when saving");

            // update page header
            page.PositionID = uint.MaxValue;
            page.Header.TransactionID = this.TransactionID;
            page.Header.IsConfirmed = i == (dirtyPages.Length - 1);
        }

        // write pages on disk and flush data
        await _disk.WriteLogPagesAsync(dirtyPages);

        // update allocation map with all dirty pages
        _allocationMap.UpdateMap(dirtyPages);

        // add pages to cache or decrement sharecount
        foreach(var page in _localPages.Values)
        {
            // page already in cache (was not changed)
            if (page.ShareCounter > 0)
            {
                page.Return();
            }
            else
            {
                // try add this page in cache
                var added = _memoryCache.AddPageInCache(page);

                if (!added)
                {
                    _bufferFactory.DeallocatePage(page);
                }
            }
        }

        // update wal index with this new version
        var pagePositions = dirtyPages
            .Select(x => (x.Header.PageID, x.PositionID));

        _walIndex.AddVersion(this.ReadVersion, pagePositions);

    }

    public void Rollback()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        if (_lockCounter == 0) return; // no locks

        while (_lockCounter > 1)
        {
            _lock.ExitCollectionWriteLock(_writeCollections[_lockCounter - 2]);
            _lockCounter--;
        }

        // exit lock transaction
        _lock.ExitTransaction();

        _lockCounter--;

        ENSURE(_lockCounter == 0, "missing release lock in transaction");
    }
}