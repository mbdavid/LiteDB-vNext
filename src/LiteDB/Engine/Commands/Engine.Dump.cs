namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async ValueTask Dump(int pageID)
    {
        var monitorService = _factory.MonitorService;
        var allocationMapService = _factory.AllocationMapService;

        if (_factory.State != EngineState.Open) throw ERR("must be closed");

        // create a new transaction with no collection lock
        var transaction = await monitorService.CreateTransactionAsync(Array.Empty<byte>());

        await transaction.InitializeAsync();

        PageBuffer page;

        // when looking for an AMP, get from AllocationMapService current instance
        if (pageID % AM_MAP_PAGES_COUNT == 0)
        {
            var allocationMapID = AllocationMapPage.GetAllocationMapID(pageID);

            page = allocationMapService.GetPageBuffer(allocationMapID);
        }
        else
        {
            page = await transaction.GetPageAsync(pageID, false);
        }

        var dump = page.DumpPage();

        Console.WriteLine(dump);

        transaction.Rollback();

        monitorService.ReleaseTransaction(transaction);
    }
}