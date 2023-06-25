namespace LiteDB.Engine;

[AutoInterface]
internal class Cursor : ICursor
{
    // dependency injections
    private readonly QueryDef _query;

    public Guid CursorId { get; } = Guid.NewGuid();
    public int ReadVersion { get; }
    public int FetchCount { get; private set; } = 0;
    public int Offset { get; private set; }
    public bool Eof { get; private set; }

    private readonly IIndexEnumerator _indexEnumerator;
    private readonly IFilterEnumerator _filterEnumerator;
    private readonly IOffsetEnumerator _offsetEnumerator;
    private readonly ILimitEnumerator _limitEnumerator;
    private readonly ISelectEnumerator _selectEnumerator;

    public Cursor(QueryDef query, int readVersion)
    {
        _query = query;

        _indexEnumerator = query.CreateIndexEnumerator();

        _filterEnumerator = new FilterEnumerator(query.Filters, _indexEnumerator, query.Collation);
        _offsetEnumerator = new OffsetEnumerator(query.Offset, _filterEnumerator);
        _limitEnumerator = new LimitEnumerator(query.Limit, _offsetEnumerator);
        _selectEnumerator = new SelectEnumerator(query.Select, _limitEnumerator, _query.Collation);
    }


    async ValueTask<FetchResult> FetchAsync(int fetchSize, ITransaction transacion, IServicesFactory factory)
    {
        var i = 0;
        var list = new List<BsonDocument>();

        while (i < fetchSize)
        {
            var doc = await _selectEnumerator.MoveNextAsync(transacion, factory);

            if (doc is null)
            {
                break;
            }
            else
            {
                list.Add(doc);
                i++;
            }
        }

        var result = new FetchResult
        {
            From = this.Offset,
            To = this.Offset + i,
            FetchCount = i,
            Eof = this.Eof,
            Results = list
        };

        this.Offset += i; 

        return result;
    }
}
