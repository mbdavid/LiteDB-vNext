namespace LiteDB.Engine;

internal class Cursor
{
    public Guid CursorId { get; }
    public int ReadVersion { get; }
   
    public int TotalFetch { get; private set; }

    private readonly int _fetchCount;

    private bool _isInitialized;


    async ValueTask Initialize(ITransaction transacion, IServiceFactory factory)
    {
        _queryEnumerator = new();
        await _queryEnumerator.Initialize(ITransaction transacion, IServiceFactory factory)
    }


    async ValueTask<FetchResult> Fetch(ITransaction transacion, IServiceFactory factory)
    {
        var i = 0;

        while (i++ < FetchCount)
        {
            var doc = await _queryEnumerator.MoveNext(transaction, factory)
        }


    }
}
