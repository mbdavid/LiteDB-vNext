namespace LiteDB.Engine;

internal class QueryOptimization : IQueryOptimization
{
    private readonly IMasterService _masterService;
    private readonly ISortService _sortService;
    private readonly IServicesFactory _factory;

    // dependency injections
    private readonly MasterDocument _master;
    private readonly CollectionDocument _collection;
    private readonly Query _query;
    private readonly BsonDocument _queryParameters;
    private readonly Collation _collation;

    // fields filled by all query optimization proccess
    private List<BinaryBsonExpression> _terms = new();
    private IDocumentLookup? _documentLookup;

    private BsonExpression? _indexExpression;
    private int _indexOrder = Query.Ascending;
    
    private BsonExpression? _filter;
    private OrderBy? _orderBy;

    public QueryOptimization(
        IMasterService masterService,
        ISortService sortService,
        MasterDocument master, 
        CollectionDocument collection, 
        Query query, 
        BsonDocument queryParameters,
        Collation collation)
    {
        _masterService = masterService;
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
        this.SplitWhereInTerms();

        this.DefineIndex();


        // create pipe enumerator based on query optimization
        return this.CreatePipeEnumerator();
    }

    #region Split Where

    /// <summary>
    /// Fill terms from where predicate list
    /// </summary>
    private void SplitWhereInTerms()
    {
        // check all where predicate for AND operators
        split(_query.Where, _terms);

        static void split(BsonExpression predicate, List<BinaryBsonExpression> terms)
        {
            if (predicate is BinaryBsonExpression bin)
            {
                if (bin.IsPredicate || bin.Type == BsonExpressionType.Or)
                {
                    terms.Add(bin);
                }
                else if (bin.Type == BsonExpressionType.And)
                {
                    split(bin.Left, terms);
                    split(bin.Right, terms);
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

    #region Index Selector

    private void DefineIndex()
    {
        // from term predicate list, get lower index cost
        var lower = this.GetLowerCostIndex();

        if (lower is null)
        {
            // if there is no index, let's from order by (if exists) or get PK
            var allIndexes = _collection.Indexes.Values;

            var selectedIndex = 
                (_query.OrderBy.IsEmpty ? null : allIndexes.FirstOrDefault(x => x.Expression == _query.OrderBy.Expression)) ??
                _collection.PK;

            _indexExpression = selectedIndex.Expression;
        }
        else
        {
            // create filter removing lower cost index predicate
            _filter = _terms.Count > 1 ? 
                BsonExpression.And(_terms.Where(x => x != lower)) : 
                null;

            _indexExpression = lower;
        }
    }

    private BinaryBsonExpression? GetLowerCostIndex()
    {
        var lowerCost = int.MaxValue;
        BinaryBsonExpression? lowerExpr = null;

        foreach (var term in _terms) 
        {
            var indexDocument =
                _collection.Indexes.Values.FirstOrDefault(x => x.Expression == term.Left) ??
                _collection.Indexes.Values.FirstOrDefault(x => x.Expression == term.Right);

            if (indexDocument is not null)
            {
                var cost = (term.Type, indexDocument.Unique) switch
                {
                    (BsonExpressionType.Equal, true) => 1,
                    (BsonExpressionType.Equal, false) => 10,
                    (BsonExpressionType.In, _) => 20,
                    (BsonExpressionType.Like, _) => 40,
                    (BsonExpressionType.GreaterThan, _) => 50,
                    (BsonExpressionType.GreaterThanOrEqual, _) => 50,
                    (BsonExpressionType.LessThan, _) => 50,
                    (BsonExpressionType.LessThanOrEqual, _) => 50,
                    (BsonExpressionType.Between, _) => 50,
                    (BsonExpressionType.NotEqual, _) => 80,
                    (_, _) => 100
                };

                if (cost < lowerCost)
                {
                    lowerCost = cost;
                    lowerExpr = term;
                }
            }
        }

        return lowerExpr;
    }

    #endregion

    private IPipeEnumerator CreatePipeEnumerator()
    {
        var pipe = _factory.CreatePipelineBuilder(_collection.Name, _queryParameters);

        pipe.AddIndex(_indexExpression!, _indexOrder);

        if (_filter is not null)
        {
            pipe.AddFilter(_filter);
        }

        if (!_query.Select.IsEmpty)
        {
            pipe.AddTransform(_query.Select);
        }

        return pipe.GetPipeEnumerator();
    }
}
