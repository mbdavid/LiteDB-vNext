namespace LiteDB.Engine;

internal class AggregateQueryOptimization : IQueryOptimization
{
    // dependency injections
    private readonly IServicesFactory _factory;

    // ctor 
    private readonly CollectionDocument _collection;

    // fields filled by all query optimization proccess

    public AggregateQueryOptimization(
        IServicesFactory factory,
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
