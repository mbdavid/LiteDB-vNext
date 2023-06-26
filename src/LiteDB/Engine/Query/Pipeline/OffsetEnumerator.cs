namespace LiteDB.Engine;

internal class OffsetEnumerator<T> : IPipeEnumerator<T>
{
    private readonly IPipeEnumerator<T> _enumerator;

    private readonly int _offset;

    private int _count = 0;
    private bool _eof = false;

    public OffsetEnumerator(int offset, IPipeEnumerator<T> enumerator)
    {
        _offset = offset;
        _enumerator = enumerator;
    }

    public async ValueTask<T?> MoveNextAsync(IDataService dataService, IIndexService indexService)
    {
        if (_eof || _offset == 0) return default; // by-pass when offset is not used

        while (_count <= _offset)
        {
            var skiped = await _enumerator.MoveNextAsync(dataService, indexService);

            if (this.IsEmpty(skiped))
            {
                _eof = true;

                return this.ReturnEmpty();
            }

            _count++;
        }

        var next = await _enumerator.MoveNextAsync(dataService, indexService);

        if (this.IsEmpty(next))
        {
            _eof = true;

            return this.ReturnEmpty();
        }

        return next;
    }

    public bool IsEmpty(T? item) => item is PageAddress addr ? addr.IsEmpty : item is null;

    public T? ReturnEmpty() => typeof(T) == typeof(PageAddress) ? (T)(object)PageAddress.Empty : default;
}
