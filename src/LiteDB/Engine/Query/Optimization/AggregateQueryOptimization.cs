namespace LiteDB.Engine;

internal class AggregateQueryOptimization : IQueryOptimization
{
    // dependency injections
    private readonly I__ServicesFactory _factory;

    // ctor 
    private readonly CollectionDocument _collection;

    // fields filled by all query optimization proccess

    public AggregateQueryOptimization(
        I__ServicesFactory factory,
        CollectionDocument collection)
    {
        _factory = factory;
        _collection = collection;
    }

    public IPipeEnumerator ProcessQuery(IQuery query, BsonDocument queryParameters)
    {
        throw new NotImplementedException();
    }
}
