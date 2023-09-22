namespace LiteDB.Engine;

public struct EngineResult : IEngineResult
{
    private readonly int _result;
    private readonly Exception? _exception;

    private TimeSpan _elapsed = TimeSpan.Zero;

    public bool Ok => _exception is null;
    public bool Fail => _exception is not null;
    public Exception? Exception => _exception!;

    public int RequestID { get; }
    public TimeSpan Elapsed { get; }

    public EngineResult()
    {
        _result = 0;
        _exception = null;
        _elapsed = TimeSpan.Zero;
    }

    public EngineResult(Exception ex)
    {
        _result = 0;
        _exception = ex;
        _elapsed = TimeSpan.Zero;
    }

    public EngineResult(int result)
    {
        _result = 0;
        _exception = null;
    }


}
