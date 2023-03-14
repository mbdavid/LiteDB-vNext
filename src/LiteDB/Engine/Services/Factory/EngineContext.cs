namespace LiteDB;

[AutoInterface(typeof(IDisposable))]
internal partial class EngineContext : IEngineContext
{
    public long StartTime = DateTime.UtcNow.Ticks;

    public int PageReadCount;
    public int PageWriteCount;

    public void Dispose()
    {
    }
}