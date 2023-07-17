namespace LiteDB.Engine;

internal class OrderByEnumerator : IPipeEnumerator
{
    // dependency injections
    private readonly ISortOperation _sorter;

    private readonly IPipeEnumerator _enumerator;
    private bool _init;

    public OrderByEnumerator(
        OrderBy orderBy,
        IPipeEnumerator enumerator,
        ISortService sortService)
    {
        _enumerator = enumerator;

        if (_enumerator.Emit.RowID == false) throw ERR($"OrderBy pipe enumerator requires rowID from last pipe");
        if (_enumerator.Emit.Document == false) throw ERR($"OrderBy pipe enumerator requires document from last pipe");

        _sorter = sortService.CreateSort(orderBy);
    }

    public PipeEmit Emit => new(true, false);

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if(_init == false)
        {
            // consume all _enumerator and get ready for new enumerator: _sorter
            await _sorter.InsertDataAsync(_enumerator, context);

            _init = true;
        }

        // get next sorted item (returns Empty when EOF)
        var item = await _sorter.MoveNextAsync();

        return new PipeValue(item.RowID);
    }

    public void Dispose()
    {
        // dispose/release all used containers
        _sorter.Dispose();
    }
}