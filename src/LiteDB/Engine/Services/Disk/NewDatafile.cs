namespace LiteDB.Engine;

[AutoInterface]
internal class NewDatafile : INewDatafile
{
    private readonly IServicesFactory _factory;
    private readonly IMemoryCacheService _memoryCache;
    private readonly IEngineSettings _settings;

    public NewDatafile(IServicesFactory factory)
    {
        _factory = factory;
        _memoryCache = factory.GetMemoryCache();
        _settings = factory.Settings;
    }

    public async Task<FileHeader> CreateAsync(IDiskStream stream)
    {
        var fileHeader = new FileHeader(_settings);

        await stream.CreateAsync(fileHeader);




        //var mapBuffer = _memoryCache.AllocateNewBuffer();
        //var masterBuffer = _memoryCache.AllocateNewBuffer();

        //var mapPage = new AllocationMapPage(AM_FIRST_PAGE_ID, mapBuffer);

        //mapPage.UpdateHeaderBuffer();


        //await stream.WriteFileHeaderAsync(fileBuffer);
        //await stream.WritePageAsync(mapBuffer);

        //// return arrays to memory
        //ArrayPool<byte>.Shared.Return(fileBuffer);
        //_memoryCache.DeallocateBuffer(mapBuffer);
        return fileHeader;

    }
}