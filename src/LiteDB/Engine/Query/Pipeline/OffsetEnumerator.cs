namespace LiteDB.Engine;

[AutoInterface(typeof(IPipeEnumerator))]
internal class OffsetEnumerator : IOffsetEnumerator
{
    private readonly IPipeEnumerator _enumerator;

    private readonly int _offset;

    private int _count = 0;
    private bool _eof = false;

    public OffsetEnumerator(int offset, IPipeEnumerator enumerator)
    {
        _offset = offset;
        _enumerator = enumerator;
    }

    public async ValueTask<BsonDocument?> MoveNextAsync(IDataService dataService, IIndexService indexService)
    {
        if (_eof || _offset == 0) return null; // by-pass when offset is not used

        while(_count <= _offset)
        {
            var skiped = await _enumerator.MoveNextAsync(dataService, indexService);

            if (skiped is null)
            {
                _eof = true;
                return null;
            }

            _count++;
        }

        var doc = await _enumerator.MoveNextAsync(transaction, dataService, indexService);

        if (doc is null)
        {
            _eof = true;
            return null;
        }

        return doc;
    }
}
