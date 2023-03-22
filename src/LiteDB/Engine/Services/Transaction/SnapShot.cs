namespace LiteDB.Engine;

/// <summary>
/// Represent a single snapshot
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class Snapshot : ISnapshot
{
    // dependency injection
    private readonly IServicesFactory _factory;
    private readonly ILockService _lock;
    private readonly IWalIndexService _walIndex;
    private readonly IAllocationMapService _allocationMap;

    private readonly int _readVersion;
    private readonly LockMode _mode;
    private readonly byte _colID;

    // local page cache - contains only data/index pages about this collection
    private readonly IDictionary<uint, DataPage> _dataPages = new Dictionary<uint, DataPage>();
    private readonly IDictionary<uint, IndexPage> _indexPages = new Dictionary<uint, IndexPage>();

    public Snapshot(IServicesFactory factory, byte colID, LockMode mode, int readVersion)
    {
        _factory = factory;
        _lock = factory.GetLock();
        _walIndex = factory.GetWalIndex();
        _allocationMap = factory.GetAllocationMap();

        _colID = colID;
        _mode = mode;
        _readVersion = readVersion;
    }

    /// <summary>
    /// Enter in write mode for this collection if snapshot are LockMode = Write
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_mode == LockMode.Write)
        {
            await _lock.EnterCollectionWriteLock(_colID);
        }
    }


    /*
        // instances from Engine
        private readonly HeaderPage _header;
        private readonly LockService _locker;
        private readonly DiskReader _reader;
        private readonly IAllocationMapService _allocationMap;
        private readonly IWalIndexService _walIndex;

        // instances from transaction
        private readonly uint _transactionID;
        private readonly TransactionPages _transPages;

        // snapshot controls
        private readonly int _readVersion;
        private readonly LockMode _mode;
        private readonly int _colID;
        private readonly string _collectionName;
        private readonly CollectionPage _collectionPage;

        // local page cache - contains only pages about this collection (but do not contains CollectionPage - use this.CollectionPage)
        private readonly IDictionary<uint, BlockPage> _localPages = new Dictionary<uint, BlockPage>();

        // expose
        public LockMode Mode => _mode;
        public string CollectionName => _collectionName;
        public ICollection<BlockPage> LocalPages => _localPages.Values;
        public int ReadVersion => _readVersion;

        public Snapshot(LockMode mode, string collectionName, HeaderPage header, uint transactionID, TransactionPages transPages, LockService locker, WalIndexService walIndex, DiskReader reader, bool addIfNotExists)
        {
            _mode = mode;
            _collectionName = collectionName;
            _header = header;
            _transactionID = transactionID;
            _transPages = transPages;
            _locker = locker;
            _walIndex = walIndex;
            _reader = reader;

            // enter in lock mode according initial mode
            if (mode == LockMode.Write)
            {
                _locker.EnterLock(_collectionName);
            }

            // get lastest read version from wal-index
            _readVersion = _walIndex.CurrentReadVersion;

            var srv = new CollectionService(_header, this, _transPages);

            // read collection (create if new - load virtual too)
            srv.Get(_collectionName, addIfNotExists, ref _collectionPage);

            // clear local pages (will clear _collectionPage link reference)
            if (_collectionPage != null)
            {
                // local pages contains only data/index pages
                _localPages.Remove(_collectionPage.PageID);
            }
        }

        /// <summary>
        /// Get all snapshot pages (can or not include collectionPage) - If included, will be last page
        /// </summary>
        public IEnumerable<BasePage> GetWritablePages(bool dirty, bool includeCollectionPage)
        {
            // if snapshot is read only, just exit
            if (_mode == LockMode.Read) yield break;

            foreach(var page in _localPages.Values.Where(x => x.IsDirty == dirty))
            {
                ENSURE(page.PageType != PageType.Header && page.PageType != PageType.Collection, "local cache cann't contains this page type");

                yield return page;
            }

            if (includeCollectionPage && _collectionPage != null && _collectionPage.IsDirty == dirty)
            {
                yield return _collectionPage;
            }
        }

        /// <summary>
        /// Clear all local pages and return page buffer to file reader. Do not release CollectionPage (only in Dispose method)
        /// </summary>
        public void Clear()
        {
            // release pages only if snapshot are read only
            if (_mode == LockMode.Read)
            {
                // release all read pages (except collection page)
                foreach (var page in _localPages.Values)
                {
                    page.Buffer.Release();
                }
            }

            _localPages.Clear();
        }

        /// <summary>
        /// Dispose stream readers and exit collection lock
        /// </summary>
        public void Dispose()
        {
            // release all data/index pages
            this.Clear();

            // release collection page (in read mode)
            if (_mode == LockMode.Read && _collectionPage != null)
            {
                _collectionPage.Buffer.Release();
            }

            if(_mode == LockMode.Write)
            {
                _locker.ExitLock(_collectionName);
            }
        }

        #region Page Version functions

        /// <summary>
        /// Get a a valid page for this snapshot (must consider local-index and wal-index)
        /// </summary>
        public T GetPage<T>(uint pageID)
            where T : BasePage
        {
            return this.GetPage<T>(pageID, out var origin, out var position, out var walVersion);
        }

        /// <summary>
        /// Get a a valid page for this snapshot (must consider local-index and wal-index)
        /// </summary>
        public T GetPage<T>(uint pageID, out long position, out int walVersion)
            where T : BasePage
        {
            ENSURE(pageID <= _header.LastPageID, "request page must be less or equals lastest page in data file");

            // check for header page (return header single instance)
            //TODO: remove this
            if (pageID == 0)
            {
                origin = FileOrigin.None;
                position = 0;
                walVersion = 0;

                return (T)(object)_header;
            }

            // look for this page inside local pages
            if (_localPages.TryGetValue(pageID, out var page))
            {
                origin = FileOrigin.None;
                position = 0;
                walVersion = 0;

                return (T)page;
            }

            // if page is not in local cache, get from disk (log/wal/data)
            page = this.ReadPage<T>(pageID, out origin, out position, out walVersion);

            // add into local pages
            _localPages[pageID] = page;

            // increment transaction size counter
            _transPages.TransactionSize++;

            return (T)page;
        }

        /// <summary>
        /// Read page from disk (dirty, wal or data)
        /// </summary>
        private T ReadPage<T>(uint pageID, out long position, out int walVersion)
            where T : BasePage
        {
            // if not inside local pages can be a dirty page saved in log file
            if (_transPages.DirtyPages.TryGetValue(pageID, out var walPosition))
            {
                // read page from log file
                var buffer = _reader.ReadPage(walPosition.Position, _mode == LockMode.Write, FileOrigin.Log);
                var dirty = BasePage.ReadPage<T>(buffer);

                origin = FileOrigin.Log;
                position = walPosition.Position;
                walVersion = _readVersion;

                ENSURE(dirty.TransactionID == _transactionID, "this page must came from same transaction");

                return dirty;
            }

            // now, look inside wal-index
            var pos = _walIndex.GetPageIndex(pageID, _readVersion, out walVersion);

            if (pos != long.MaxValue)
            {
                // read page from log file
                var buffer = _reader.ReadPage(pos, _mode == LockMode.Write, FileOrigin.Log);
                var logPage = BasePage.ReadPage<T>(buffer);

                // clear some data inside this page (will be override when write on log file)
                logPage.TransactionID = 0;
                logPage.IsConfirmed = false;

                origin = FileOrigin.Log;
                position = pos;

                return logPage;
            }
            else
            {
                // for last chance, look inside original disk data file
                var pagePosition = BasePage.GetPagePosition(pageID);

                // read page from data file
                var buffer = _reader.ReadPage(pagePosition, _mode == LockMode.Write, FileOrigin.Data);
                var diskpage = BasePage.ReadPage<T>(buffer);

                origin = FileOrigin.Data;
                position = pagePosition;

                ENSURE(diskpage.IsConfirmed == false || diskpage.TransactionID != 0, "page are not header-clear in data file");

                return diskpage;
            }
        }


        /// <summary>
        /// Returns a page that contains space enough to data to insert new object - if one does not exit, creates a new page.
        /// Before return page, fix empty free list slot according with passed length
        /// </summary>
        public DataPage GetFreeDataPage(int bytesLength)
        {
        }

        /// <summary>
        /// Get a index page with space enouth for a new index node
        /// </summary>
        public IndexPage GetFreeIndexPage(int bytesLength, ref uint freeIndexPageList)
        {
        }


        #endregion


        public override string ToString()
        {
            return $"{_collectionName} (pages: {_localPages.Count})";
        }
    */
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}