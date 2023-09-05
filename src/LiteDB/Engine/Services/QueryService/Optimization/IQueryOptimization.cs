namespace LiteDB.Engine;

internal interface IQueryOptimization
{
    IPipeEnumerator ProcessQuery(IQuery query, BsonDocument queryParameters);
}
