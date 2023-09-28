namespace LiteDB.Engine;

[AutoInterface]
internal class QueryOptimization : IQueryOptimization
{
    // dependency injections
    protected readonly IServicesFactory _factory;

    // fields filled by all query optimization proccess
    protected IDocumentStore? _store;

    // SlitWhere
    protected List<BinaryBsonExpression> _terms = new();

    // Define Index
    protected int _indexCost = 0;
    protected BsonExpression _indexExpression = BsonExpression.Empty;
    protected int _indexOrder = Query.Ascending;

    // Define Filter
    protected BsonExpression _filter = BsonExpression.Empty;

    // Define OrderBy
    protected OrderBy _orderBy = OrderBy.Empty;

    // Define Includes (Before/After)
    protected List<BsonExpression> _includesBefore = new();
    protected List<BsonExpression> _includesAfter = new();

    // Define lookups
    protected IDocumentLookup? _documentLookup;
    protected IDocumentLookup? _orderByLookup;

    public QueryOptimization(IServicesFactory factory)
    {
        _factory = factory;
    }

    public virtual IPipeEnumerator ProcessQuery(Query query, BsonDocument queryParameters)
    {
        // get document store and initialize
        _store = _factory.StoreFactory.GetUserCollection(query.Collection);

        _store.Initialize(_factory.MasterService);

        // split where expressions into TERMs (splited by AND operator)
        this.SplitWhereInTerms(query.Where);

        // get lower cost index or pk index
        this.DefineIndexAndFilter(query.OrderBy);

        // define _orderBy field (or use index order)
        this.DefineOrderBy(query.OrderBy);

        // define where includes must be called (before/after) orderby/filter
        this.DefineIncludes(query);

        // define lookup for index/order by
        this.DefineLookups(query);

        // create pipe enumerator based on query optimization
        return this.CreatePipeEnumerator(query, queryParameters);
    }

    private IPipeEnumerator CreatePipeEnumerator(Query query, BsonDocument queryParameters)
    {
        var pipe = _factory.CreatePipelineBuilder(_store!, queryParameters);

        pipe.AddIndex(_indexExpression!, _indexOrder);

        pipe.AddLookup(_documentLookup!);

        foreach (var include in _includesBefore)
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

        if (!query.Select.IsRoot)
            pipe.AddTransform(query.Select);

        return pipe.GetPipeEnumerator();
    }

    #region Split Where

    /// <summary>
    /// Fill terms from where predicate list
    /// </summary>
    protected virtual void SplitWhereInTerms(BsonExpression predicate)
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

    private void DefineIndexAndFilter(OrderBy orderBy)
    {
        // from term predicate list, get lower term that can be use as best index option
        var (lowerCost, lowerExpr, lowerIndex) = this.GetLowerCostIndex();

        var allIndexes = _store!.GetIndexes();

        _indexCost = lowerCost;

        if (lowerExpr is null)
        {
            // if there is no index, let's from order by (if exists) or get PK
            var pk = allIndexes[0];

            var selectedIndex = 
                (orderBy.IsEmpty ? null : allIndexes.FirstOrDefault(x => x.Expression == orderBy.Expression)) ??
                allIndexes[0];

            _indexExpression = selectedIndex.Expression;
        }
        else
        {
            _indexExpression = lowerExpr;

            _terms.RemoveAt(lowerIndex);
        }

        // after define index, create filter with terms
        if (_terms.Count > 0)
        {
            _filter = BsonExpression.And(_terms);
        }
    }

    private (int cost, BinaryBsonExpression? expr, int index) GetLowerCostIndex()
    {
        var lowerCost = int.MaxValue;
        var lowerIndex = -1;
        var indexes = _store!.GetIndexes();
        BinaryBsonExpression? lowerExpr = null;

        for(var i = 0; i < _terms.Count; i++)
        {
            var term = _terms[i];

            var indexDocument =
                indexes.FirstOrDefault(x => x.Expression == term.Left) ??
                indexes.FirstOrDefault(x => x.Expression == term.Right);

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
                    lowerIndex = i;
                }
            }
        }

        return (lowerCost, lowerExpr, lowerIndex);
    }

    #endregion

    #region OrderBy

    protected virtual void DefineOrderBy(OrderBy orderBy)
    {
        if (orderBy.IsEmpty) return;
        if (_indexExpression is null) return;

        // if query expression are same used in index, has no need use orderBy
        // suport left only expression - right only value
        if (_indexExpression is BinaryBsonExpression bin && orderBy.Expression == bin.Left)
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
        if (query.Includes.Count == 0) return;

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
            if (fields.Length == 1 && fields[0] == _indexExpression.ToString()![2..])
            {
                // use index based document lookup
                _documentLookup = new IndexLookup(fields[0]);
            }
            else
            {
                _documentLookup = new DataLookup(fields);
            }
        }

        // with OrderBy
        else
        {
            // get all fields used before order by
            var docFields = this.GetFields(query, where: true, orderBy: true, before: true);

            // if contains a single field and are index expression
            if (docFields.Length == 1 && docFields[0] == _indexExpression.ToString()![2..])
            {
                // use index based document lookup
                _documentLookup = new IndexLookup(docFields[0]);
            }
            else
            {
                _documentLookup = new DataLookup(docFields);
            }

            // get all fields used after order by
            var orderFields = this.GetFields(query, select: true, before: true);

            // if contains a single field and are index expression
            if (orderFields.Length == 1 && orderFields[0] == _indexExpression.ToString()![2..])
            {
                _orderByLookup = new IndexLookup(orderFields[0]);
            }
            else
            {
                _orderByLookup = new DataLookup(orderFields);
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

        if (where && add(query.Where, fields)) return Array.Empty<string>();
        if (orderBy && add(query.OrderBy.Expression, fields)) return Array.Empty<string>();

        if (select)
        {
            if (query.Select.IsSingleExpression)
            {
                if (add(query.Select.SingleExpression, fields)) return Array.Empty<string>();
            }
            else
            {
                foreach(var field in query.Select.Fields)
                {
                    if (add(field.Expression, fields)) return Array.Empty<string>();
                }
            }
        }

        if (before)
        {
            foreach (var expr in _includesBefore)
            {
                if (add(expr, fields)) return Array.Empty<string>();
            }
        }

        if (after)
        {
            foreach (var expr in _includesAfter)
            {
                if (add(expr, fields)) return Array.Empty<string>();
            }
        }

        return fields.ToArray();

        static bool add(BsonExpression expr, HashSet<string> fields)
        {
            var info = expr.GetInfo();

            if (info.FullRoot) return true;

            fields.AddRange(info.RootFields);

            return false;
        }
    }

    #endregion
}
