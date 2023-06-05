namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
internal class LogTempDisk
{
    private uint _lastPositionID;

    private Dictionary<uint, uint> _tempPages = new();

    public LogTempDisk(uint logEndPositionID)
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
    public uint GetTempPositionID(uint logPositionID)
    {
        if (_tempPages.TryGetValue(logPositionID, out var tempPositionID))
        {
            return tempPositionID;
        }

        return uint.MaxValue;
    }
}