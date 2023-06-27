namespace LiteDB.Engine;

internal class LimitEnumerator : IPipeEnumerator
{
    private readonly IPipeEnumerator _enumerator;

    private readonly int _limit;

    private int _count = 0;
    private bool _eof = false;

    public LimitEnumerator(int limit, IPipeEnumerator enumerator)
    {
        _limit = limit;
        _enumerator = enumerator;
    }

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var item = await _enumerator.MoveNextAsync(context);

        if (item.IsEmpty)
        {
            _eof = true;
            return PipeValue.Empty;
        }

        _count++;

        if (_count >= _limit)
        {
            _eof = true;
        }

        return item;
    }

    public void Dispose()
    {
    }
}
