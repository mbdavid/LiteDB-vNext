namespace LiteDB.Engine;

internal class AggregateQueryOptimization : IQueryOptimization
{
    // dependency injections
    private readonly IServicesFactory _factory;

    // ctor 
    private readonly CollectionDocument _collection;
    private readonly AggregateQuery _query;
    private readonly BsonDocument _queryParameters;

    // fields filled by all query optimization proccess

    public AggregateQueryOptimization(
        IServicesFactory factory,
        CollectionDocument collection,
        AggregateQuery query,
        BsonDocument queryParameters)
    {
        _factory = factory;
        _collection = collection;
        _query = query;
        _queryParameters = queryParameters;
    }

    public IPipeEnumerator ProcessQuery()
    {
        throw new NotImplementedException();
    }
}
