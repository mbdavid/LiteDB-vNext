namespace LiteDB.Engine;

internal class LookupEnumerator : IPipeEnumerator
{
    private readonly IDocumentLookup _lookup;
    private readonly IPipeEnumerator _enumerator;

    private bool _eof = false;

    public LookupEnumerator(IDocumentLookup lookup, IPipeEnumerator enumerator)
    {
        _lookup = lookup;
        _enumerator = enumerator;

        if (_enumerator.Emit.RowID == false) throw ERR($"Lookup pipe enumerator requires rowID from last pipe");
    }

    public PipeEmit Emit => new(true, true);

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var item = await _enumerator.MoveNextAsync(context);

        if (item.IsEmpty)
        {
            _eof = true;
            return PipeValue.Empty;
        }

        var doc = await _lookup.LoadAsync(item, context);

        return new PipeValue(item.RowID, doc);
    }

    public void Dispose()
    {
    }
}
