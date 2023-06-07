namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface]
internal class LogService : ILogService
{
    // dependency injection
    private readonly IBufferFactory _bufferFactory;
    private readonly ICacheService _cacheService;
    private readonly IWalIndexService _walIndexService;

    private int _lastPageID;
    private int _logPositionID;

    private readonly List<PageHeader> _logPages = new();
    private readonly HashSet<int> _confirmedTransactions = new();

    public LogService(
        ICacheService cacheService,
        IBufferFactory bufferFactory,
        IWalIndexService walIndexService)
    {
        _cacheService = cacheService;
        _bufferFactory = bufferFactory;
        _walIndexService = walIndexService;
    }

    public void Initialize(int lastFilePositionID)
    {
        _lastPageID = lastFilePositionID;

        _logPositionID = lastFilePositionID + 5; //TODO: calcular para proxima extend
    }

    /// <summary>
    /// Get next positionID in log
    /// </summary>
    public int GetNextLogPositionID()
    {
        var next = Interlocked.Increment(ref _logPositionID);

        // test if next log position is not an AMP
        if (next % AM_EXTEND_COUNT == 0) next = Interlocked.Increment(ref _logPositionID);

        return next;
    }

    /// <summary>
    /// Add a page header in log list, to be used in checkpoint operation.
    /// This page should be added here after write on disk
    /// </summary>
    public void AddLogPage(PageHeader header)
    {
        // if page is confirmed, set transaction as confirmed and ok to override on data file
        if (header.IsConfirmed)
        {
            _confirmedTransactions.Add(header.TransactionID);
        }

        // update _lastPageID
        if (header.PageID > _lastPageID)
        {
            _lastPageID = header.PageID;
        }

        _logPages.Add(header);
    }

    public async Task<int> CheckpointAsync(IDiskService disk, LogTempDisk? tempPages)
    {
        var logLength = _logPages.Count;

        if (logLength == 0) return 0;

        ENSURE(_logPositionID == _logPages[^1].PositionID, $"last log page must positionID = {_logPositionID}");

        // get writer stream from disk service
        var stream = disk.GetDiskWriter();

        // get a temp or create a new 
        var temp = tempPages ?? new LogTempDisk(_logPositionID);

        for (var i = 0; i < _logPages.Count; i++)
        {
            var header = _logPages[i];

            // check if page is in a confirmed transaction
            if (!_confirmedTransactions.Contains(header.TransactionID)) continue;

            // checks if is temp log
            var tempPositionID = temp.GetTempPositionID(header.PositionID);

            // get file from log position or temp position
            var filePositionID = tempPositionID == int.MaxValue ?
                header.PositionID : tempPositionID;

            // if page positionID is less than pageID, copy to temp buffer
            if (header.PositionID < header.PageID)
            {
                // get page from cache or disk
                var targetPage = await this.GetLogPageAsync(stream, header.PageID);

                // copy target page to buffer
                temp.AddNewPage(targetPage);

                // write page to destination
                await stream.WritePageAsync(targetPage);

                //TODO: conferir como desalocar a pagina
            }

            // get page from file position ID (log or 
            var page = await this.GetLogPageAsync(stream, filePositionID);

            // move page from log to data
            page.PositionID = page.Header.PositionID = page.Header.PageID;
            page.Header.TransactionID = 0;
            page.Header.IsConfirmed = false;

            // write page on disk
            await stream.WritePageAsync(page);

            // and now can re-enter to cache
            _cacheService.AddPageInCache(page);

            // if this log 
            var needEmpty = filePositionID <= _lastPageID;

            if (needEmpty) await stream.WriteEmptyAsync(filePositionID);
        }

        // crop file after lastPageID
        stream.SetSize(_lastPageID);

        // empty all wal index pointer
        _walIndexService.Clear();

        // clear all log pages
        _logPages.Clear();

        // disk flush
        await stream.FlushAsync();

        return logLength;
    }

    /// <summary>
    /// Get page from cache (remove if found) or create a new from page factory
    /// </summary>
    private async Task<PageBuffer> GetLogPageAsync(IDiskStream stream, int positionID)
    {
        // try get page from cache
        var page = _cacheService.TryRemove(positionID);

        if (page is null)
        {
            page = _bufferFactory.AllocateNewPage(true);

            await stream.ReadPageAsync(positionID, page);
        }

        return page;
    }
}