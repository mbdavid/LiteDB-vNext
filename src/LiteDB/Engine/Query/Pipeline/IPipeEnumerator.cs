namespace LiteDB.Engine;

internal interface IPipeEnumerator
{
    ValueTask<BsonDocument?> MoveNextAsync(IDataService dataService, IIndexService indexService);
}
