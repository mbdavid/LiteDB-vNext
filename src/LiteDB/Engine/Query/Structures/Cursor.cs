namespace LiteDB.Engine;

internal class Cursor
{
    // dependency injections
    public Query Query { get; }

    public Guid CursorID { get; } = Guid.NewGuid();
    public IPipeEnumerator Enumerator { get; }
    public int ReadVersion { get; }

    public int FetchCount { get; set; } = 0;
    public int Offset { get; set; } = 0;
    public bool IsRunning { get; set; } = false;
    public bool Eof { get; set; } = false;
    public DateTime Start { get; } = DateTime.UtcNow;
    public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

    // tempo em execucao/inicio/fim/

    public Cursor(Query query, IPipeEnumerator enumerator, int readVersion)
    {
        this.Query = query;
        this.ReadVersion = readVersion;
        this.Enumerator = enumerator;
    }
}
