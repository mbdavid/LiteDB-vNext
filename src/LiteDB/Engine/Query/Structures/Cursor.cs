namespace LiteDB.Engine;

internal class Cursor : IDisposable
{
    public Guid CursorID { get; } = Guid.NewGuid();

    public Query Query { get; }
    public int ReadVersion { get; }
    public IPipeEnumerator Enumerator { get; }

    public int FetchCount { get; set; } = 0;
    public int Offset { get; set; } = 0;
    public bool IsRunning { get; set; } = false;
    public bool Eof { get; set; } = false;

    public DateTime Start { get; } = DateTime.UtcNow;
    public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

    public Cursor(Query query, int readVersion, IPipeEnumerator enumerator)
    {
        this.Query = query;
        this.ReadVersion = readVersion;
        this.Enumerator = enumerator;
    }

    public void Dispose()
    {
        this.Enumerator.Dispose();
    }
}
