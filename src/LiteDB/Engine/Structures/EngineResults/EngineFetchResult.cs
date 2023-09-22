namespace LiteDB.Engine;

public struct EngineFetchResult : IEngineResult
{
    public int From;
    public int To;
    public int FetchCount;
    public bool HasMore;
    public IReadOnlyCollection<BsonDocument> Results;



    public int RequestID { get; }
    public TimeSpan Elapsed { get; }
    public bool Ok { get; }
    public bool Fail { get; }
    public Exception? Exception { get; }

    public override string ToString() => Dump.Object(this);
}
