namespace LiteDB.Engine;

internal interface IPipelineEnumerator
{
    ValueTask<BsonDocument?> MoveNextAsync(ITransaction transacion, IServicesFactory factory);
}
