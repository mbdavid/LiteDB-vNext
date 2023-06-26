namespace LiteDB.Engine;

[AutoInterface(typeof(IPipeEnumerator))]
internal class LookupEnumerator : ILookupEnumerator
{
    private readonly IDocumentLookup _lookup;
    private readonly IIndexEnumerator _enumerator;

    private readonly int _limit;

    private int _count = 0;
    private bool _eof = false;

    public LookupEnumerator(IDocumentLookup lookup, IIndexEnumerator enumerator)
    {
        _lookup = lookup;
        _enumerator = enumerator;
    }

    public async ValueTask<BsonDocument?> MoveNextAsync(IDataService dataService, IIndexService indexService)
    {
        if (_eof || _limit == int.MaxValue) return null; // by-pass when limit is not used

        var dataBlock = await _enumerator.MoveNextAsync(indexService);

        if (dataBlock.IsEmpty)
        {
            _eof = true;
            return null;
        }

        _count++;

        if (_count >= _limit)
        {
            _eof = true;
        }

        var doc = await _lookup.LoadAsync(dataBlock, dataService);

        return doc;
    }
}
