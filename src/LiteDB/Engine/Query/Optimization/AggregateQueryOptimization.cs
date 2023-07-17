namespace LiteDB.Engine;

internal class AggregateQueryOptimization : IQueryOptimization
{
    private readonly ISortService _sortService;

    // dependency injections
    private readonly MasterDocument _master;
    private readonly CollectionDocument _collection;
    private readonly AggregateQuery _query;
    private readonly BsonDocument _queryParameters;
    private readonly Collation _collation;

    // fields filled by all query optimization proccess
    private List<BinaryBsonExpression> _terms = new();
    private IndexCost? _selectedIndex;
    private IDocumentLookup? _documentLookup;
    private BsonExpression? _filter;
    private (BsonExpression expr, int order, IDocumentLookup lookup)? _orderBy;

    public AggregateQueryOptimization(
        ISortService sortService,
        MasterDocument master, 
        CollectionDocument collection, 
        AggregateQuery query, 
        BsonDocument queryParameters,
        Collation collation)
    {
        _sortService = sortService;
        _master = master;
        _collection = collection;
        _query = query;
        _queryParameters = queryParameters;
        _collation = collation;
    }

    public IPipeEnumerator ProcessQuery()
    {
        // split where expressions into TERMs (splited by AND operator)
        this.SplitWherePredicateInTerms();

        // do terms optimizations
        this.OptimizeTerms();

        // create pipe enumerator based on query optimization
        return this.CreatePipeEnumerator();
    }

    #region Split Where

    /// <summary>
    /// Fill terms from where predicate list
    /// </summary>
    private void SplitWherePredicateInTerms()
    {
        void add(BsonExpression predicate)
        {
            if (predicate is BinaryBsonExpression bin)
            {
                if (bin.IsPredicate || bin.Type == BsonExpressionType.Or)
                {
                    _terms.Add(bin);
                }
                else if (bin.Type == BsonExpressionType.And)
                {
                    add(bin.Left);
                    add(bin.Right);
                }
                else
                {
                    throw ERR($"Invalid WHERE expression: {predicate}");
                }
            }
            else
            {
                throw ERR($"Invalid WHERE expression: {predicate}");
            }
        }

        // check all where predicate for AND operators
        add(_query.Where);
    }

    /// <summary>
    /// Do some pre-defined optimization on terms to convert expensive filter in indexable filter
    /// </summary>
    private void OptimizeTerms()
    {
        // simple optimization
        for (var i = 0; i < _terms.Count; i++)
        {
            var term = _terms[i];

            // TODO???
        }
    }

    #endregion

    #region Document Fields

    /// <summary>
    /// Load all fields that must be deserialize from document.
    /// </summary>
    private void DefineQueryFields()
    {
        // load only query fields (null return all document)
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    }

    #endregion

    private IPipeEnumerator CreatePipeEnumerator()
    {
        throw new NotImplementedException();
    }
}
