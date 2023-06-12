namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
internal class LogTempDisk
{
    private int _lastPositionID;

    private Dictionary<int, int> _tempPages = new();

    public int Count => _tempPages.Count;

    public int LastPositionID => _lastPositionID;

    public LogTempDisk(int logEndPositionID)
    {
        _lastPositionID = logEndPositionID - 1;
    }

    /// <summary>
    /// Add a page into temp log. If page are new, add in next position
    /// </summary>
    public void AddNewPage(PageBuffer page)
    {
        // test if page already exists
        if (page.PositionID == page.Header.PositionID)
        {
            page.PositionID = ++_lastPositionID;
        }

        // copy header position (log position) to file position
        _tempPages[page.Header.PositionID] = page.PositionID;
    }

    /// <summary>
    /// Get page position (in temp area) using logPositionID. Returns MaxValue if not found
    /// </summary>
    public int GetTempPositionID(int logPositionID)
    {
        if (_tempPages.TryGetValue(logPositionID, out var tempPositionID))
        {
            return tempPositionID;
        }

        return int.MaxValue;
    }
}