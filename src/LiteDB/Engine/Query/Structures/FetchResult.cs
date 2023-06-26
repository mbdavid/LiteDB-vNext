namespace LiteDB.Engine;

public struct FetchResult
{
    public int From;
    public int To;
    public int FetchCount;
    public bool Eof;
    public IReadOnlyCollection<BsonDocument> Results;
}
