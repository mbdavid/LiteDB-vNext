namespace LiteDB.Engine;

internal interface IIndexEnumerator
{
    ValueTask<BsonDocument?> MoveNextAsync(ITransaction transacion, IServicesFactory factory);
}
