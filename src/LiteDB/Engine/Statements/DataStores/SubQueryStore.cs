namespace LiteDB.Engine;

internal class SubQueryStore : ISourceStore
{
    public string Name { get; }

    private Query _subQuery;

    public SubQueryStore(Query query)
    {
        _subQuery = query;
    }

    internal void Loa0d(IMasterService masterService)
    {
    }
    
    public IPipeEnumerator GetPipeEnumerator(BsonExpression expression)
    {
        throw new NotImplementedException();
    }
}
