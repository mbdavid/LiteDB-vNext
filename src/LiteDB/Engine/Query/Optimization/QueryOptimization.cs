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

        // get lower cost index or pk index
        this.DefineIndex();

        // define _orderBy field (or use index order)
        this.DefineOrderBy();

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

    #region OrderBy

    private void DefineOrderBy()
    {
        if (_query.OrderBy.IsEmpty) return;
        if (_indexExpression is null) return;

        // if query expression are same used in index, has no need use orderBy
        if (_query.OrderBy.Expression == _indexExpression)
        {
            _indexOrder = _query.OrderBy.Order;
        }
        else
        {
            _orderBy = _query.OrderBy;
        }
    }

    #endregion

    #region Includes

    /// <summary>
    /// Will define each include to be run BEFORE where (worst) OR AFTER where (best)
    /// </summary>
    private void DefineIncludes()
    {
        foreach (var include in _query.Includes)
        {
            var info = new BsonExpressionInfo(include);

            //info.RootFields
            //
            //// includes always has one single field
            //var field = include.Fields.Single();
            //
            //// test if field are using in any filter or orderBy
            //var used = _queryPlan.Filters.Any(x => x.Fields.Contains(field)) ||
            //    (_queryPlan.OrderBy?.Expression.Fields.Contains(field) ?? false);
            //
            //if (used)
            //{
            //    _queryPlan.IncludeBefore.Add(include);
            //}
            //
            //// in case of using OrderBy this can eliminate IncludeBefre - this need be added in After
            //if (!used || _queryPlan.OrderBy != null)
            //{
            //    _queryPlan.IncludeAfter.Add(include);
            //}
        }
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
