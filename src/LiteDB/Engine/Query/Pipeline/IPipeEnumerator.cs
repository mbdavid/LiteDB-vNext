namespace LiteDB.Engine;

/// <summary>
/// Interface for a custom query pipe. Return null (for BsonDocument) or PageAddress.Empty when EOF
/// </summary>
/// <typeparam name="T">BsonDocument or PageAddress</typeparam>
internal interface IPipeEnumerator<T>
{
    ValueTask<T?> MoveNextAsync(IDataService dataService, IIndexService indexService);
}
