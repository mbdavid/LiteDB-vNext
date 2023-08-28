namespace LiteDB.Engine;

public struct FetchResult
{
    public int From;
    public int To;
    public int FetchCount;
    public bool HasMore;
    public IReadOnlyCollection<BsonDocument> Results;

    public FetchResult(int from, int to, int fetchCount, bool hasMore, IReadOnlyCollection<BsonDocument> results)
    {
        this.From = from;
        this.To = to;
        this.FetchCount = fetchCount;
        this.HasMore = hasMore;
        this.Results = results;
    }

    public override string ToString()
    {
        return Dump.Object(new { From, To, FetchCount, HasMore, Results });
    }
}
