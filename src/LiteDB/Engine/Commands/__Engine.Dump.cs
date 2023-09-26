namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async ValueTask DumpAsync(int pageID)
    {
        //var monitorService = _factory.MonitorService;
        //var allocationMapService = _factory.AllocationMapService;

        //if (_factory.State != EngineState.Open) throw ERR("must be closed");

        //// create a new transaction with no collection lock
        //var transaction = await monitorService.CreateTransactionAsync(Array.Empty<byte>());

        //await transaction.InitializeAsync();

        //PageBuffer page;

        //// when looking for an AMP, get from AllocationMapService current instance
        //if (pageID % AM_MAP_PAGES_COUNT == 0)
        //{
        //    var allocationMapID = __AllocationMapPage.GetAllocationMapID(pageID);

        //    page = allocationMapService.GetPageBuffer(allocationMapID);
        //}
        //else
        //{
        //    page = await transaction.GetPageAsync(pageID);
        //}

        //var dump = page.DumpPage();

        //Console.WriteLine(dump);

        //transaction.Rollback();

        //monitorService.ReleaseTransaction(transaction);
    }

    public void DumpState(string? headerTitle = null)
    {
        var monitorService = _factory.MonitorService;
        var allocationMapService = _factory.AllocationMapService;
        var memoryFactory = _factory.MemoryFactory;
        var memoryCache = _factory.MemoryCache;
        var lockService = _factory.LockService;
        var logService = _factory.LogService;
        var diskService = _factory.DiskService;

        var dump = Dump.Object(new { monitorService, memoryFactory, allocationMapService, memoryCache, lockService, logService, diskService });

        if (headerTitle is not null)
        {
            Console.WriteLine("= " + headerTitle);
        }

        Console.WriteLine(dump);

    }
}