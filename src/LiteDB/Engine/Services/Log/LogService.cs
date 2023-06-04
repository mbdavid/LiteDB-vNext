namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface]
internal class LogService : ILogService
{
    private struct TempPage
    {
        public readonly uint LogPositionID;
        public readonly uint FilePositionID;

        public override int GetHashCode() => (int)this.LogPositionID;
    }


    // dependency injection

    private uint _logStartPositionID;
    private int _logEndPositionID;
    private bool _init = false;

    private List<PageHeader> _pageHeaders = new();
    private HashSet<int> _confirmedTransactions = new();
    private HashSet<TempPage> _tempPages = new();

    public void Initialize(uint lastFilePositionID)
    {
        _init = true;

        _logStartPositionID = lastFilePositionID + 5; //TODO: calcular para proxima extend

        _logEndPositionID = (int)_logStartPositionID;

        _pageHeaders.Clear();
        _confirmedTransactions.Clear();
        _tempPages.Clear();
    }

    ///// <summary>
    ///// Get start log position ID
    ///// </summary>
    //public uint LogStartPositionID => _logStartPositionID;

    ///// <summary>
    ///// Get end log position ID
    ///// </summary>
    //public uint LogEndPositionID => (uint)_logEndPositionID;

    /// <summary>
    /// Get next positionID in log
    /// </summary>
    public uint GetNextLogPositionID()
    {
        // do not increment on first time
        if (_init)
        {
            _init = false;
            return _logStartPositionID;
        }

        return (uint)Interlocked.Increment(ref _logEndPositionID);
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

        _pageHeaders.Add(header);
    }

    public async Task<int> Checkpoint(IDiskService disk)
    {
        
        for(var i = 0; i < _pageHeaders.Count; i++)
        {
            var header = _pageHeaders[i];

            // check if page is in a confirmed transaction
            if (!_confirmedTransactions.Contains(header.TransactionID)) continue;

            // se a pagina for pra tras ou igual, copia
            if (header.PositionID <= header.PageID)
            {


                //await disk.WriteDataPage()
            }
            else
            {
                // adiciona no tempDisk
            }



        }

        // for do temp disk

    }
}