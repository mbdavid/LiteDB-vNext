namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface]
internal class LogService : ILogService
{
    // dependency injection
    private readonly IDiskService _disk;

    private uint _logStartPositionID;
    private int _logEndPositionID;

    public LogService(IDiskService disk)
    {
        _disk = disk;
    }

    /// <summary>
    /// Get start log position ID
    /// </summary>
    public uint LogStartPositionID => _logStartPositionID;

    /// <summary>
    /// Get end log position ID
    /// </summary>
    public uint LogEndPositionID => (uint)_logEndPositionID;

    /// <summary>
    /// Get next positionID in log
    /// </summary>
    public uint GetNextLogPositionID()
    {
        if (_logStartPositionID == 0 && _logEndPositionID == 0)
        {
            _logStartPositionID = _disk.GetLastFilePositionID() + 5; //TODO: calcular para proxima extend

            _logEndPositionID = (int)_logStartPositionID;

            return _logStartPositionID;
        }

        return (uint)Interlocked.Increment(ref _logEndPositionID);
    }
}