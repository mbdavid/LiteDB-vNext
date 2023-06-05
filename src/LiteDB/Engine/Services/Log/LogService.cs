using System.IO;
using System.IO.Pipes;

namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface]
internal class LogService : ILogService
{
    // dependency injection
    private readonly IBufferFactory _bufferFactory;
    private readonly IMemoryCacheService _memoryCache;
    private readonly IWalIndexService _walIndex;

    private uint _lastPageID;
    private int _logPositionID;

    private readonly List<PageHeader> _logPages = new();
    private readonly HashSet<int> _confirmedTransactions = new();

    public LogService(
        IMemoryCacheService memoryCache,
        IBufferFactory bufferFactory,
        IWalIndexService walIndex)
    {
        _memoryCache = memoryCache;
        _bufferFactory = bufferFactory;
        _walIndex = walIndex;
    }

    public void Initialize(uint lastFilePositionID)
    {
        _lastPageID = lastFilePositionID;

        _logPositionID = (int)lastFilePositionID + 5; //TODO: calcular para proxima extend
    }

    /// <summary>
    /// Get next positionID in log
    /// </summary>
    public uint GetNextLogPositionID()
    {
        return (uint)Interlocked.Increment(ref _logPositionID);
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
        if (_confirmedTransactions.Count == 0) return 0;

        var logEndPositionID = _logPages[^1].PositionID;

        // get writer stream from disk service
        var stream = disk.GetDiskWriter();

        // get a temp or create a new 
        var temp = tempPages ?? new LogTempDisk(logEndPositionID);

        for (var i = 0; i < _logPages.Count; i++)
        {
            var header = _logPages[i];

            // check if page is in a confirmed transaction
            if (!_confirmedTransactions.Contains(header.TransactionID)) continue;

            // checks if is temp area
            var tempPositionID = temp.GetPagePositionID(header.PositionID);

            // get file from log position or temp position
            var filePositionID = tempPositionID == uint.MaxValue ?
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
            _memoryCache.AddPageInCache(page);

            // if this log 
            var needEmpty = filePositionID <= _lastPageID;

            if (needEmpty) await stream.WriteEmptyAsync(filePositionID);
        }

        // crop file after lastPageID
        stream.SetSize(_lastPageID);

        // empty all wal index pointer
        _walIndex.Clear();

        // clear all log pages
        _logPages.Clear();

        // disk flush
        await stream.FlushAsync();

        return 1;
    }

    /// <summary>
    /// Get page from cache (remove if found) or create a new from page factory
    /// </summary>
    private async Task<PageBuffer> GetLogPageAsync(IDiskStream stream, uint positionID)
    {
        // try get page from cache
        var page = _memoryCache.TryRemove(positionID);

        if (page is null)
        {
            page = _bufferFactory.AllocateNewPage(true);

            await stream.ReadPageAsync(positionID, page);
        }

        return page;
    }
}