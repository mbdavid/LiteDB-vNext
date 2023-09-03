namespace LiteDB.Engine;

internal class QueryOptimization : IQueryOptimization
{
    // dependency injections
    private readonly I__ServicesFactory _factory;

    // ctor 
    private readonly CollectionDocument _collection;

    // fields filled by all query optimization proccess

    // SlitWhere
    private List<BinaryBsonExpression> _terms = new();

    // Define Index
    private int _indexCost = 0;
    private BsonExpression _indexExpression = BsonExpression.Empty;
    private int _indexOrder = Query.Ascending;
    private BsonExpression _filter = BsonExpression.Empty;

    // Define OrderBy
    private OrderBy _orderBy = OrderBy.Empty;

    // Define Includes (Before/After)
    private List<BsonExpression> _includesBefore = new();
    private List<BsonExpression> _includesAfter = new();

    // Define lookups
    private IDocumentLookup? _documentLookup;
    private IDocumentLookup? _orderByLookup;

    public QueryOptimization(
        I__ServicesFactory factory,
        CollectionDocument collection)
    {
        _factory = factory;
        _collection = collection;
    }

    public IPipeEnumerator ProcessQuery(IQuery query, BsonDocument queryParameters)
    {
        var plainQuery = (Query)query;

        // split where expressions into TERMs (splited by AND operator)
        this.SplitWhereInTerms(plainQuery.Where);

        // get lower cost index or pk index
        this.DefineIndex(plainQuery.OrderBy);

        // define _orderBy field (or use index order)
        this.DefineOrderBy(plainQuery.OrderBy);

        // define where includes must be called (before/after) orderby/filter
        this.DefineIncludes(plainQuery);

        // define lookup for index/order by
        this.DefineLookups(plainQuery);

        // create pipe enumerator based on query optimization
        return this.CreatePipeEnumerator(plainQuery, queryParameters);
    }

    #region Split Where

    /// <summary>
    /// Fill terms from where predicate list
    /// </summary>
    private void SplitWhereInTerms(BsonExpression predicate)
    {
        if (predicate.IsEmpty) return;

        if (predicate is BinaryBsonExpression bin)
        {
            if (bin.IsPredicate || bin.Type == BsonExpressionType.Or)
            {
                _terms.Add(bin);
            }
            else if (bin.Type == BsonExpressionType.And)
            {
                this.SplitWhereInTerms(bin.Left);
                this.SplitWhereInTerms(bin.Right);
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

    #endregion

    #region Index Selector

    private void DefineIndex(OrderBy orderBy)
    {
        // from term predicate list, get lower index cost
        var (cost, lower) = this.GetLowerCostIndex();

        _indexCost = cost;

        if (lower is null)
        {
            // if there is no index, let's from order by (if exists) or get PK
            var allIndexes = _collection.Indexes;

            var selectedIndex = 
                (orderBy.IsEmpty ? null : allIndexes.FirstOrDefault(x => x.Expression == orderBy.Expression)) ??
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

    private (int, BinaryBsonExpression?) GetLowerCostIndex()
    {
        var lowerCost = int.MaxValue;
        BinaryBsonExpression? lowerExpr = null;

        foreach (var term in _terms) 
        {
            var indexDocument =
                _collection.Indexes.FirstOrDefault(x => x.Expression == term.Left) ??
                _collection.Indexes.FirstOrDefault(x => x.Expression == term.Right);

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

        return (lowerCost, lowerExpr);
    }

    #endregion

    #region OrderBy

    private void DefineOrderBy(OrderBy orderBy)
    {
        if (orderBy.IsEmpty) return;
        if (_indexExpression is null) return;

        // if query expression are same used in index, has no need use orderBy
        if (orderBy.Expression == _indexExpression)
        {
            _indexOrder = orderBy.Order;
        }
        else
        {
            _orderBy = orderBy;
        }
    }

    #endregion

    #region Includes

    /// <summary>
    /// Will define each include to be run BEFORE where (worst) OR AFTER where (best)
    /// </summary>
    private void DefineIncludes(Query query)
    {
        if (query.Includes is null || query.Includes.Length == 0) return;

        var infoWhere = query.Where.GetInfo();
        var infoOrderBy = query.OrderBy.Expression.GetInfo();

        foreach (var include in query.Includes)
        {
            var info = include.GetInfo();

            // includes always has one single field
            var field = info.RootFields.Single();

            // test if field are using in any filter or orderBy
            var used =
                infoWhere.RootFields.Contains(field, StringComparer.OrdinalIgnoreCase) ||
                infoOrderBy.RootFields.Contains(field, StringComparer.OrdinalIgnoreCase) ||
                false;

            if (used)
            {
                _includesBefore.Add(include);
            }
            
            // in case of using OrderBy this can eliminate IncludeBefre - this need be added in After
            if (!used || !_orderBy.IsEmpty)
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
    private void DefineLookups(Query query)
    {
        // without OrderBy
        if (_orderBy.IsEmpty)
        {
            // get all root fiels using in this query (empty means need load full document)
            var fields = this.GetFields(query, where: true, select: true, orderBy: true, before: true, after: true);

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
            var docFields = this.GetFields(query, where: true, orderBy: true, before: true);

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
            var orderFields = this.GetFields(query, select: true, before: true);

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
    /// Get all fields used in many expressions (used bool to avoid new array)
    /// </summary>
    private string[] GetFields(
        Query query,
        bool where = false, 
        bool select = false, 
        bool orderBy = false, 
        bool before = false, 
        bool after = false)
    {
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (add(where, query.Where, fields)) return Array.Empty<string>();
        if (add(select, query.Select, fields)) return Array.Empty<string>();
        if (add(orderBy, query.OrderBy.Expression, fields)) return Array.Empty<string>();

        if (before)
        {
            foreach (var expr in _includesBefore)
            {
                if (add(true, expr, fields)) return Array.Empty<string>();
            }
        }

        if (after)
        {
            foreach (var expr in _includesAfter)
            {
                if (add(true, expr, fields)) return Array.Empty<string>();
            }
        }

        return fields.ToArray();

        static bool add(bool conditional, BsonExpression expr, HashSet<string> fields)
        {
            if (!conditional) return false;

            var info = expr.GetInfo();

            if (info.FullRoot) return true;

            fields.AddRange(info.RootFields);

            return false;
        }
    }

    #endregion

    private IPipeEnumerator CreatePipeEnumerator(Query query, BsonDocument queryParameters)
    {
        var pipe = _factory.CreatePipelineBuilder(_collection.Name, queryParameters);

        pipe.AddIndex(_indexExpression!, _indexOrder);

        pipe.AddLookup(_documentLookup!);

        foreach(var include in _includesBefore)
            pipe.AddInclude(include);

        if (_filter.IsEmpty == false)
            pipe.AddFilter(_filter);

        if (_orderBy.IsEmpty == false)
            pipe.AddOrderBy(_orderBy);

        if (query.Offset > 0)
            pipe.AddOffset(query.Offset);

        if (query.Limit != int.MaxValue)
            pipe.AddLimit(query.Limit);

        if (_orderByLookup is not null)
            pipe.AddLookup(_orderByLookup);

        foreach (var include in _includesAfter)
            pipe.AddInclude(include);

        if (query.Select.IsEmpty == false)
            pipe.AddTransform(query.Select);

        return pipe.GetPipeEnumerator();
    }
}
