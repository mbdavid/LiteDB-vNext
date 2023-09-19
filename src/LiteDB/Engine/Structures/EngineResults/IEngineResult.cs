namespace LiteDB.Engine;

public interface IEngineResult
{
    public int RequestID { get; }
    public TimeSpan Elapsed { get; }
    public bool Ok { get; }
    public bool Fail { get; }
    public Exception? Exception { get; }
}
