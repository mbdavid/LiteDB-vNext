namespace LiteDB.Engine;

internal class AggregateQueryOptimization : IQueryOptimization
{
    private readonly ISortService _sortService;

    // dependency injections
    private readonly MasterDocument _master;
    private readonly CollectionDocument _collection;
    private readonly AggregateQuery _query;
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
        Collation collation)
    {
        _sortService = sortService;
        _master = master;
        _collection = collection;
        _query = query;
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
        // create index enumerator
        var indexEnumerator = _selectedIndex!.Value.CreateIndex();

        // create query pipeline based on enumerators order
        var pipeEnumerator = new LookupEnumerator(_documentLookup!, indexEnumerator) as IPipeEnumerator;

        if (_filter is not null)
        {
            pipeEnumerator = new FilterEnumerator(_filter, _collation, pipeEnumerator);
        }

        if (_orderBy is not null)
        {
            pipeEnumerator = new OrderByEnumerator(_sortService, _orderBy.Value.expr, _orderBy.Value.order, pipeEnumerator);

            if (_query.Offset > 0)
            {
                pipeEnumerator = new OffsetEnumerator(_query.Offset, pipeEnumerator);
            }

            if (_query.Limit != int.MaxValue)
            {
                pipeEnumerator = new LimitEnumerator(_query.Limit, pipeEnumerator);
            }

            pipeEnumerator = new LookupEnumerator(_orderBy.Value.lookup, pipeEnumerator);
        }
        else
        {
            if (_query.Offset > 0)
            {
                pipeEnumerator = new OffsetEnumerator(_query.Offset, pipeEnumerator);
            }

            if (_query.Limit != int.MaxValue)
            {
                pipeEnumerator = new LimitEnumerator(_query.Limit, pipeEnumerator);
            }
        }

        if (_query.Select.Type != BsonExpressionType.Empty)
        {
            pipeEnumerator = new TransformEnumerator(_query.Select, _collation, pipeEnumerator);
        }

        return pipeEnumerator;
    }
}
