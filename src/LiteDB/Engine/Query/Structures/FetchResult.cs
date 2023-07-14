namespace LiteDB.Engine;

public struct FetchResult
{
    public int From;
    public int To;
    public int FetchCount;
    public bool HasMore;
    public IReadOnlyCollection<BsonDocument> Results;
}
