namespace LiteDB.Engine;

internal class Cursor : IDisposable, IIsEmpty
{
    public Guid CursorID { get; private init; } = Guid.NewGuid();

    public Query Query { get; }
    public BsonDocument Parameters { get; }
    public int ReadVersion { get; }
    public IPipeEnumerator Enumerator { get; }

    public int FetchCount { get; set; } = 0;
    public int Offset { get; set; } = 0;
    public bool IsRunning { get; set; } = false;

    public DateTime Start { get; } = DateTime.UtcNow;
    public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

    public BsonDocument? NextDocument { get; set; }

    public bool IsEmpty => this.CursorID == Guid.Empty;

    public static Cursor Empty = new Cursor();

    public Cursor()
    {
        this.CursorID = Guid.Empty;
        this.Parameters = BsonDocument.Empty;
        this.ReadVersion = 0;
        this.Enumerator = null;
    }

    public Cursor(Query query, BsonDocument parameters, int readVersion, IPipeEnumerator enumerator)
    {
        this.Query = query;
        this.Parameters = parameters;
        this.ReadVersion = readVersion;
        this.Enumerator = enumerator;
    }

    public void Dispose()
    {
        this.Enumerator.Dispose();
    }
}
