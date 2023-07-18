namespace LiteDB.Engine;

internal class QueryOptimization : IQueryOptimization
{
    // dependency injections
    private readonly IServicesFactory _factory;

    // ctor 
    private readonly CollectionDocument _collection;
    private readonly Query _query;
    private readonly BsonDocument _queryParameters;

    // fields filled by all query optimization proccess
    private readonly BsonExpressionInfo _infoWhere;
    private readonly BsonExpressionInfo _infoOrderBy;
    private readonly BsonExpressionInfo _infoSelect;

    private List<BinaryBsonExpression> _terms = new();

    private BsonExpression _indexExpression = BsonExpression.Empty;
    private int _indexOrder = Query.Ascending;

    private BsonExpression _filter = BsonExpression.Empty;
    private List<BsonExpression> _includesBefore = new();
    private List<BsonExpression> _includesAfter = new();
    private OrderBy _orderBy = OrderBy.Empty;

    private IDocumentLookup? _documentLookup;
    private IDocumentLookup? _orderByLookup;

    public QueryOptimization(
        IServicesFactory factory,
        CollectionDocument collection, 
        Query query, 
        BsonDocument queryParameters)
    {
        _factory = factory;

        _collection = collection;
        _query = query;
        _queryParameters = queryParameters;

        _infoSelect = new BsonExpressionInfo(_query.Select);
        _infoWhere = new BsonExpressionInfo(_query.Where);
        _infoOrderBy = new BsonExpressionInfo(_query.OrderBy.Expression);
    }

    public IPipeEnumerator ProcessQuery()
    {
        // split where expressions into TERMs (splited by AND operator)
        this.SplitWhereInTerms();

        // get lower cost index or pk index
        this.DefineIndex();

        // define _orderBy field (or use index order)
        this.DefineOrderBy();

        // define where includes must be called (before/after) orderby/filter
        this.DefineIncludes();

        // define lookup for index/order by
        this.DefineLookups();

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
            if (_terms.Count > 1)
            {
                _filter = BsonExpression.And(_terms.Where(x => x != lower));
            }

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

            // includes always has one single field
            var field = info.RootFields.Single();

            // test if field are using in any filter or orderBy
            var used =
                _infoWhere.RootFields.Contains(field, StringComparer.OrdinalIgnoreCase) ||
                _infoOrderBy.RootFields.Contains(field, StringComparer.OrdinalIgnoreCase) ||
                false;

            if (used)
            {
                _includesBefore.Add(include);
            }
            
            // in case of using OrderBy this can eliminate IncludeBefre - this need be added in After
            if (!used || !_query.OrderBy.IsEmpty)
            {
                _includesAfter.Add(include);
            }
        }
    }

    #endregion

    #region Lookup

    /// <summary>
    /// Define both lookups, for index and order by pipe enumerator
    /// </summary>
    private void DefineLookups()
    {
        // without OrderBy
        if (_orderBy.IsEmpty)
        {
            // get all root fiels using in this query (empty means need load full document)
            var fields = this.GetFields(
                new BsonExpressionInfo[]
                {
                    _infoWhere,
                    _infoOrderBy,
                    _infoSelect
                }
                .Union(_includesBefore.Select(i => new BsonExpressionInfo(i)))
                .Union(_includesAfter.Select(i => new BsonExpressionInfo(i))));

            // if contains a single field and are index expression
            if (fields.Length == 1 && fields[0] == _indexExpression.ToString()[2..])
            {
                // use index based document lookup
                _documentLookup = new IndexServiceLookup(fields[0]);
            }
            else
            {
                _documentLookup = new DataServiceLookup(fields);
            }
        }

        // with OrderBy
        else
        {
            // get all fields used before order by
            //TODO: implementar melhor... simplificar
            var docFields = this.GetFields(
                new BsonExpressionInfo[]
                {
                    _infoWhere,
                    _infoOrderBy,
                }
                .Union(_includesBefore.Select(i => new BsonExpressionInfo(i))));

            // if contains a single field and are index expression
            if (docFields.Length == 1 && docFields[0] == _indexExpression.ToString()[2..])
            {
                // use index based document lookup
                _documentLookup = new IndexServiceLookup(docFields[0]);
            }
            else
            {
                _documentLookup = new DataServiceLookup(docFields);
            }

            // get all fields used after order by
            var orderFields = this.GetFields(
                new BsonExpressionInfo[]
                {
                    _infoSelect,
                }
                .Union(_includesBefore.Select(i => new BsonExpressionInfo(i))));

            // if contains a single field and are index expression
            if (orderFields.Length == 1 && orderFields[0] == _indexExpression.ToString()[2..])
            {
                _orderByLookup = new IndexServiceLookup(orderFields[0]);
            }
            else
            {
                _orderByLookup = new DataServiceLookup(orderFields);
            }
        }

    }

    /// <summary>
    /// Get all fields used in many expressions
    /// </summary>
    private string[] GetFields(IEnumerable<BsonExpressionInfo> infos)
    {
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var info in infos)
        {
            if (info.FullRoot) return Array.Empty<string>();

            fields.AddRange(info.RootFields);
        }

        return fields.ToArray();
    }

    #endregion

    private IPipeEnumerator CreatePipeEnumerator()
    {
        var pipe = _factory.CreatePipelineBuilder(_collection.Name, _queryParameters);

        pipe.AddIndex(_indexExpression!, _indexOrder);

        pipe.AddLookup(_documentLookup!);

        _includesBefore.ForEach(i => pipe.AddInclude(i));

        pipe.AddFilter(_filter);

        pipe.AddOrderBy(_orderBy);

        pipe.AddOffset(_query.Offset);

        pipe.AddLimit(_query.Limit);

        if (_orderByLookup is not null)
        {
            pipe.AddLookup(_orderByLookup);
        }

        _includesAfter.ForEach(i => pipe.AddInclude(i));

        pipe.AddTransform(_query.Select);

        return pipe.GetPipeEnumerator();
    }
}
