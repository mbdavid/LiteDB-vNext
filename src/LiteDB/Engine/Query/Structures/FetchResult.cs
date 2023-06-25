namespace LiteDB.Engine;

internal struct FetchResult
{
    public int From;
    public int To;
    public int FetchCount;
    public bool Eof;
    public IList<BsonDocument> Results;
}
