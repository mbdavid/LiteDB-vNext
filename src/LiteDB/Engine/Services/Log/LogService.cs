using System.IO;

namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal partial class LogService : ILogService
{
    // dependency injection
    private readonly IDiskService _diskService;
    private readonly ICacheService _cacheService;
    private readonly IBufferFactory _bufferFactory;
    private readonly IWalIndexService _walIndexService;
    private readonly IServicesFactory _factory;

    private int _lastPageID;
    private int _logPositionID;

    private readonly List<PageHeader> _logPages = new();
    private readonly HashSet<int> _confirmedTransactions = new();

    public LogService(
        IDiskService diskService,
        ICacheService cacheService,
        IBufferFactory bufferFactory,
        IWalIndexService walIndexService,
        IServicesFactory factory)
    {
        _diskService = diskService;
        _cacheService = cacheService;
        _bufferFactory = bufferFactory;
        _walIndexService = walIndexService;
        _factory = factory;
    }

    public void Initialize(int lastFilePositionID)
    {
        _lastPageID = lastFilePositionID;

        _logPositionID = this.CalcInitLogPositionID(lastFilePositionID);
    }

    /// <summary>
    /// Get initial file log position based on next extent
    /// </summary>
    private int CalcInitLogPositionID(int lastPageID)
    {
        // add 2 extend space between lastPageID and new logPositionID
        var allocationMapID = (int)(lastPageID / AM_PAGE_STEP);
        var extendIndex = (lastPageID - 1 - allocationMapID * AM_PAGE_STEP) / AM_EXTEND_SIZE;

        var nextExtendIndex = (extendIndex + 2) % AM_EXTEND_COUNT;
        var nextAllocationMapID = allocationMapID + (nextExtendIndex < extendIndex ? 1 : 0);
        var nextPositionID = (nextAllocationMapID * AM_PAGE_STEP + nextExtendIndex * AM_EXTEND_SIZE + 1);

        return nextPositionID - 1; // first run get next()
    }

    /// <summary>
    /// </summary>
    public async Task WriteLogPagesAsync(PageBuffer[] pages)
    {
        var writer = _diskService.GetDiskWriter();

        // set IsFileDirty flag in header file to true at first use
        if (!_factory.FileHeader.IsFileDirty)
        {
            _factory.FileHeader.SetFileDirty(true);

            writer.WriteFlag(FileHeader.P_IS_FILE_DIRTY, 1);
        }

        for (var i = 0; i < pages.Length; i++)
        {
            var page = pages[i];

            ENSURE(page.PositionID == int.MaxValue, $"current page {page.PositionID} should be MaxValue");

            // get next page position on log (update header PositionID too)
            page.PositionID = this.GetNextLogPositionID();
            page.Header.PositionID = page.PositionID;

            // write page to writer stream
            await writer.WritePageAsync(page);

            // add page header only into log memory list
            this.AddLogPage(page.Header);
        }

        // flush to disk
        await writer.FlushAsync();
    }


    /// <summary>
    /// Get next positionID in log
    /// </summary>
    private int GetNextLogPositionID()
    {
        var next = Interlocked.Increment(ref _logPositionID);

        // test if next log position is not an AMP
        if (next % AM_PAGE_STEP == 0) next = Interlocked.Increment(ref _logPositionID);

        return next;
    }

    /// <summary>
    /// Add a page header in log list, to be used in checkpoint operation.
    /// This page should be added here after write on disk
    /// </summary>
    private void AddLogPage(PageHeader header)
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

    public Task<int> CheckpointAsync(bool crop)
    {
        var logLength = _logPages.Count;

        if (logLength == 0) return Task.FromResult(0);

        ENSURE(_logPositionID == _logPages[^1].PositionID, $"last log page must positionID = {_logPositionID}");

        // temp file start after lastPageID or last log used page
        var startTempPositionID = Math.Max(_lastPageID, _logPositionID) + 1;
        var tempPages = Array.Empty<PageHeader>();

        return this.CheckpointAsync(startTempPositionID, tempPages, crop);
    }

    private async Task<int> CheckpointAsync(int startTempPositionID, IList<PageHeader> tempPages, bool crop)
    {
        //** ao iniciar o checkpoint, todas as paginas da cache estão sem uso
        // o total de paginas utilizadas são: total da cache + freeBuffers

        // get all actions that checkpoint must do with all pages
        var actions = new CheckpointActions().GetActions(
            _logPages, 
            _confirmedTransactions,
            _lastPageID,
            startTempPositionID, 
            tempPages);

        // get writer stream from disk service
        var writer = _diskService.GetDiskWriter();
        var counter = 0;

        foreach (var action in actions)
        {
            // get page from file position ID (log or data)
            var page = await this.GetLogPageAsync(writer, action.PositionID);

            // at this point, "page" are NOT in cache anymore (if was in cache)

            if (action.Action == CheckpointActionEnum.ClearPage)
            {
                await writer.WriteEmptyAsync(action.PositionID);
            }
            else
            {
                if (action.Action == CheckpointActionEnum.CopyToDataFile)
                {
                    // transform this page into a data file page
                    page.Header.PositionID = page.Header.PageID = action.TargetPositionID;
                    page.Header.TransactionID = 0;
                    page.Header.IsConfirmed = false;

                    await writer.WritePageAsync(page);

                    // increment checkpoint counter page
                    counter++;
                }
                else if (action.Action == CheckpointActionEnum.CopyToTempFile)
                {
                    // transform this page into a log temp file (keeps Header.PositionID in original value)
                    page.PositionID = action.TargetPositionID;
                    page.Header.IsConfirmed = true; // mark all pages to true in temp disk (to recovery)

                    await writer.WritePageAsync(page);
                }

                // after copy page, checks if page need to be clean on disk
                if (action.MustClear)
                {
                    await writer.WriteEmptyAsync(action.PositionID);
                }
            }

            var addToCache = action.Action != CheckpointActionEnum.ClearPage;

            // test if current page should be added in cache or deallocates
            if (addToCache)
            {
                // if cache contains this position (old data version) must be removed from cache and deallocate
                if (_cacheService.TryRemove(page.PositionID, out var removedPage))
                {
                    _bufferFactory.DeallocatePage(removedPage!);
                }

                // add this page to cache (or try it)
                var added = _cacheService.AddPageInCache(page);

                // if cache is full, deallocate page (page was
                if (!added)
                {
                    _bufferFactory.DeallocatePage(page);
                }
            }
            else
            {
                // page has old/invalid data - just deallocate
                _bufferFactory.DeallocatePage(page);
            }
        }

        // crop file or fill with \0 after _lastPageID
        if (crop)
        {
            // crop file after _lastPageID
            writer.SetSize(_lastPageID);
        }
        else
        {
            // get last page (from log or from temp file)
            var lastFilePositionID = tempPages.Count > 0 ?
                startTempPositionID * tempPages.Count :
                _logPositionID;

            await writer.WriteEmptyAsync(_lastPageID + 1, lastFilePositionID);
        }

        // reset initial log position
        _logPositionID = this.CalcInitLogPositionID(_lastPageID);

        // empty all wal index pointer (there is no wal index after checkpoint)
        _walIndexService.Clear();

        // clear all log pages/confirm transactions
        _logPages.Clear();
        _confirmedTransactions.Clear();

        // ao terminar o checkpoint, nenhuma pagina na cache deve ser de log
        return counter;
    }

    /// <summary>
    /// Get page from cache (remove if found) or create a new from page factory
    /// </summary>
    private async Task<PageBuffer> GetLogPageAsync(IDiskStream stream, int positionID)
    {
        // try get page from cache
        if (_cacheService.TryRemove(positionID, out var page))
        {
            return page!;
        }

        // otherwise, allocate new buffer page and read from disk
        page = _bufferFactory.AllocateNewPage(true);

        await stream.ReadPageAsync(positionID, page);

        return page;
    }

    public void Dispose()
    {
        _logPages.Clear();
        _confirmedTransactions.Clear();
    }
}
