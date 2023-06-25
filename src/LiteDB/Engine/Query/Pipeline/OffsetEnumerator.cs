namespace LiteDB.Engine;

[AutoInterface(typeof(IPipelineEnumerator))]
internal class OffsetEnumerator : IOffsetEnumerator
{
    private readonly IPipelineEnumerator _enumerator;

    private readonly int _offset;

    private int _count = 0;
    private bool _eof = false;

    public OffsetEnumerator(int offset, IPipelineEnumerator enumerator)
    {
        _offset = offset;
        _enumerator = enumerator;
    }

    public async ValueTask<BsonDocument?> MoveNextAsync(ITransaction transacion, IServicesFactory factory)
    {
        if (_eof || _offset == 0) return null; // by-pass when offset is not used

        while(_count <= _offset)
        {
            var skiped = await _enumerator.MoveNextAsync(transacion, factory);

            if (skiped is null)
            {
                _eof = true;
                return null;
            }

            _count++;
        }

        var doc = await _enumerator.MoveNextAsync(transacion, factory);

        if (doc is null)
        {
            _eof = true;
            return null;
        }

        return doc;
    }
}
