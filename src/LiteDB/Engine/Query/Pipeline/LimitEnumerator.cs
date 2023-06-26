namespace LiteDB.Engine;

internal class LimitEnumerator<T> : IPipeEnumerator<T>
{
    private readonly IPipeEnumerator<T> _enumerator;

    private readonly int _limit;

    private int _count = 0;
    private bool _eof = false;

    public LimitEnumerator(int limit, IPipeEnumerator<T> enumerator)
    {
        _limit = limit;
        _enumerator = enumerator;
    }

    public async ValueTask<T?> MoveNextAsync(IDataService dataService, IIndexService indexService)
    {
        if (_eof || _limit == int.MaxValue) return default; // by-pass when limit is not used

        var next = await _enumerator.MoveNextAsync(dataService, indexService);

        if (this.IsEmpty(next))
        {
            _eof = true;

            return this.ReturnEmpty();
        }

        _count++;

        if (_count >= _limit)
        {
            _eof = true;
        }

        return next;
    }

    public bool IsEmpty(T? item) => item is PageAddress addr ? addr.IsEmpty : item is null;

    public T? ReturnEmpty() => typeof(T) == typeof(PageAddress) ? (T)(object)PageAddress.Empty : default;
}
