namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
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
    /// Get next positionID in log
    /// </summary>
    public int GetNextLogPositionID()
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

    public async Task<int> CheckpointAsync(IDiskService disk, int startTempPositionID, IList<PageHeader> tempPages, bool crop)
    {
        var logLength = _logPages.Count;

        if (logLength == 0) return 0;

        ENSURE(_logPositionID == _logPages[^1].PositionID, $"last log page must positionID = {_logPositionID}");

        //** ao iniciar o checkpoint, todas as paginas da cache estão sem uso
        // o total de paginas utilizadas são: total da cache + freeBuffers

        // get all actions that checkpoint must do with all pages
        var actions = this.GetCheckpointActions(_logPages, _confirmedTransactions, startTempPositionID, tempPages);

        // get writer stream from disk service
        var stream = disk.GetDiskWriter();

        foreach (var action in actions)
        {
            // get page from file position ID (log or data)
            var page = await this.GetLogPageAsync(stream, action.PositionID);

            if (action.Action == CheckpointActionEnum.ClearPage)
            {
                await stream.WriteEmptyAsync(action.PositionID);
            }
            else if (action.Action == CheckpointActionEnum.CopyToDataFile)
            {
                // transform this page into a data file page
                //page.Header.PositionID = page.Header.PageID = action.PositionID;
                //page.Header.TransactionID = 0;
                page.Header.IsConfirmed = true; // mark all pages to true in temp disk (to recovery)

                await stream.WritePageAsync(page);
            }
            else if (action.Action == CheckpointActionEnum.CopyToTempFile)
            {
                // transform this page into a log temp file
                page.PositionID = action.TargetPositionID;
                page.Header.TransactionID = 0;
                page.Header.IsConfirmed = false;

                await stream.WritePageAsync(page);
            }
        }

        // ao terminar o checkpoint, nenhuma pagina na cache deve ser de log
        return 1;

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

    public void Dispose()
    {
        _logPages.Clear();
        _confirmedTransactions.Clear();
    }

    /* ******************************************************************* */

    public IList<CheckpointAction> GetCheckpointActions(
        IReadOnlyList<PageHeader> logPages,
        HashSet<int> confirmedTransactions,
        int startTempPositionID,
        IList<PageHeader> tempPages)
    {
        //TODO: Lucas
        //throw new NotImplementedException();

        return new List<CheckpointAction>();
    }


}
