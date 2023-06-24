namespace LiteDB.Engine;

internal struct FetchResult
{
    public int From;
    public int To;
    public int TotalFetch;
    public bool Eof;
    public BsonDocument[] Results;
}
