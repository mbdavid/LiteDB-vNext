namespace LiteDB.Engine;

[AutoInterface]
internal class QueryOptimization : IQueryOptimization
{
    // dependency injections
    private readonly MasterDocument _master;
    private readonly CollectionDocument _collection;
    private readonly Query _query;
    private readonly Collation _collation;
    private List<BinaryBsonExpression> _terms = new();
    private IndexDocument _indexDocument;
    private BsonValue _indexKey;

    public QueryOptimization(MasterDocument master, CollectionDocument collection, Query query, Collation collation)
    {
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


        //-------------------------------------------------------

        var lookup = new DataServiceLookup(Array.Empty<string>());
        var indexEnumerator = new IndexEqualsEnumerator(_indexKey, _indexDocument, _collation);

        // create query pipeline based on enumerators order
        var lookupEnumerator = new LookupEnumerator(lookup, indexEnumerator);
        var filterEnumerator = new FilterEnumerator(_terms.First(), _collation, lookupEnumerator);
        var offsetEnumerator = new OffsetEnumerator(_query.Offset, filterEnumerator);
        var limitEnumerator = new LimitEnumerator(_query.Limit, offsetEnumerator);
        var selectEnumerator = new TransformEnumerator(_query.Select, _collation, limitEnumerator);

        return selectEnumerator;
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
        foreach (var predicate in _query.Where)
        {
            add(predicate);
        }
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

        if (_query.Select.Type == BsonExpressionType.em)

        // include all fields detected in all used expressions
        fields.AddRange(_query.Select.Fields);
        fields.AddRange(_terms.SelectMany(x => x.Fields));
        fields.AddRange(_query.Includes.SelectMany(x => x.Fields));
        fields.AddRange(_query.GroupBy?.Fields);
        fields.AddRange(_query.Having?.Fields);
        fields.AddRange(_query.OrderBy?.Fields);

        // if contains $, all fields must be deserialized
        if (fields.Contains("$"))
        {
            fields.Clear();
        }

        _queryPlan.Fields = fields;
    }

    #endregion


}
