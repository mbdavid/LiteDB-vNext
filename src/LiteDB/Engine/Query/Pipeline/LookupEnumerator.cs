namespace LiteDB.Engine;

internal class LookupEnumerator : IPipeEnumerator<BsonDocument>
{
    private readonly IDocumentLookup _lookup;
    private readonly IPipeEnumerator<PageAddress> _enumerator;

    private bool _eof = false;

    public LookupEnumerator(IDocumentLookup lookup, IPipeEnumerator<PageAddress> enumerator)
    {
        _lookup = lookup;
        _enumerator = enumerator;
    }

    public async ValueTask<BsonDocument?> MoveNextAsync(IDataService dataService, IIndexService indexService)
    {
        if (_eof) return null;

        var dataBlock = await _enumerator.MoveNextAsync(dataService, indexService);

        if (dataBlock.IsEmpty)
        {
            _eof = true;
            return null;
        }

        var doc = await _lookup.LoadAsync(dataBlock, dataService);

        return doc;
    }
}
