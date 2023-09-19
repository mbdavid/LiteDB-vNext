namespace LiteDB.Engine;

internal interface IFetchStatement
{
    ValueTask<FetchResult> ExecuteFetchAsync(IServicesFactory factory, BsonDocument parameters);
}
