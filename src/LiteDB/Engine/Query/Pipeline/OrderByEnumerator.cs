namespace LiteDB.Engine;

internal class OrderByEnumerator : IPipeEnumerator
{
    private readonly IPipeEnumerator _enumerator;
    private readonly ISortOperation _sorter;
    private bool _init;

    public OrderByEnumerator(
        ISortService sortService,
        BsonExpression expr, 
        int order, 
        IPipeEnumerator enumerator)
    {
        _enumerator = enumerator;

        _sorter = sortService.CreateSort(expr, order);
    }

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

        return new PipeValue(item);
    }

    public void Dispose()
    {
        // dispose/release all used containers
        _sorter.Dispose();
    }
}
