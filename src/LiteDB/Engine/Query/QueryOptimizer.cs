namespace LiteDB.Engine;

[AutoInterface]
internal class QueryOptimizer : IQueryOptimizer
{
    // dependency injections
    private readonly MasterDocument _master;
    private readonly CollectionDocument _collection;
    private readonly Query _query;
    private readonly Collation _collation;
    private IList<BsonExpression> _terms;
    private IndexDocument _indexDocument;
    private BsonValue _indexKey;

    public QueryOptimizer(MasterDocument master, CollectionDocument collection, Query query, Collation collation)
    {
        _master = master;
        _collection = collection;
        _query = query;
        _collation = collation;
    }

    public IPipeEnumerator<BsonDocument> ProcessQuery()
    {
        var lookup = new DataServiceLookup(new HashSet<string>());
        var indexEnumerator = new IndexEqualsEnumerator(_indexKey, _indexDocument, _collation);

        // create query pipeline based on enumerators order
        var lookupEnumerator = new LookupEnumerator(lookup, indexEnumerator);
        var filterEnumerator = new FilterEnumerator(_terms, lookupEnumerator, _collation);
        var offsetEnumerator = new OffsetEnumerator<BsonDocument>(_query.Offset, filterEnumerator);
        var limitEnumerator = new LimitEnumerator<BsonDocument>(_query.Limit, offsetEnumerator);
        var selectEnumerator = new TransformEnumerator(_query.Select, limitEnumerator, _collation);

        return selectEnumerator;
    }
}
