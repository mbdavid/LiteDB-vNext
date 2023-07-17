using System;
using System.Collections.Generic;

namespace LiteDB.Engine;

internal class PipelineBuilder
{
    // dependency injections
    private IMasterService _masterService;
    private ISortService _sortService;
    private Collation _collation;

    private CollectionDocument _collection;
    private BsonDocument _queryParameters;
    private IPipeEnumerator? _enumerator;

    public PipelineBuilder(
        IMasterService masterService,
        ISortService sortService,
        Collation collation,
        string collectionName, 
        BsonDocument queryParameters)
    {
        _masterService = masterService;
        _sortService = sortService;
        _collation = collation;
        _queryParameters = queryParameters;

        var master = _masterService.GetMaster(false);

        if (!master.Collections.TryGetValue(collectionName, out _collection))
        {
            throw ERR($"Collection {collectionName} doesn't exist");
        }
    }

    public IDocumentLookup CreateDocumentLookup(string[] fields)
        => new DataServiceLookup(fields);

    public IDocumentLookup CreateIndexLookup(string field)
        => new IndexServiceLookup(field);

    /// <summary>
    /// Add full index scan in some order
    /// </summary>
    public PipelineBuilder AddIndex(string indexName, int order)
    {
        return this;
    }

    /// <summary>
    /// Add index pipe based on predicate expression (must be BinaryBsonExpression). Delect indexDocument based on left/right side of predicate (should exist in collection)
    /// </summary>
    public PipelineBuilder AddIndex(BsonExpression predicate, int order)
    {
        var pred = (BinaryBsonExpression)predicate;



        return this;
    }

    /// <summary>
    /// Add a new document lookup deserializing full document
    /// </summary>
    public PipelineBuilder AddLookup()
        => this.AddLookup(Array.Empty<string>());

    /// <summary>
    /// Add a new document lookup deserializing selected fields only
    /// </summary>
    public PipelineBuilder AddLookup(string[] fields)
        => this.AddLookup(this.CreateDocumentLookup(fields));

    /// <summary>
    /// Add a new index lookup using field name
    /// </summary>
    public PipelineBuilder AddLookup(string field)
        => this.AddLookup(this.CreateIndexLookup(field));

    /// <summary>
    /// Add an existed lookup implementation
    /// </summary>
    public PipelineBuilder AddLookup(IDocumentLookup lookup)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new LookupEnumerator(lookup, _enumerator);

        return this;
    }

    public PipelineBuilder AddFilter(BsonExpression filter)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new FilterEnumerator(filter, _enumerator, _collation);

        return this;
    }

    public PipelineBuilder AddOffset(int offset)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new OffsetEnumerator(offset, _enumerator);

        return this;
    }

    public PipelineBuilder AddLimit(int limit)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new LimitEnumerator(limit, _enumerator);

        return this;
    }

    public PipelineBuilder AddOrderBy(OrderBy orderBy)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new OrderByEnumerator(orderBy, _enumerator, _sortService);

        return this;
    }

    public PipelineBuilder AddInclude(BsonExpression pathExpr)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new IncludeEnumerator(pathExpr, _enumerator, _masterService, _collation);

        return this;
    }


    public PipelineBuilder AddAggregate(IAggregateFunc[] funcs)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new AggregateEnumerator(funcs, _enumerator, _collation);

        return this;
    }

    public PipelineBuilder AddTransform(BsonExpression select)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new TransformEnumerator(select, _collation, _enumerator);

        return this;
    }
}
