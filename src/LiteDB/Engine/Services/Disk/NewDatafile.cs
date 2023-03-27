namespace LiteDB.Engine;

[AutoInterface]
internal class NewDatafile : INewDatafile
{
    private readonly IServicesFactory _factory;
    private readonly IBufferFactoryService _bufferFactory;
    private readonly IBsonWriter _writer;
    private readonly IEngineSettings _settings;

    public NewDatafile(IServicesFactory factory)
    {
        _factory = factory;
        _bufferFactory = factory.GetBufferFactory();
        _writer = _factory.GetBsonWriter();
        _settings = factory.Settings;
    }

    /// <summary>
    /// Create a empty database using user-settings as default values
    /// Create FileHeader, first AllocationMap page and first $master data page
    /// </summary>
    public async Task<FileHeader> CreateAsync(IDiskStream stream)
    {
        // initialize FileHeader with user settings
        var fileHeader = new FileHeader(_settings);

        // create new file and write header
        await stream.CreateAsync(fileHeader);

        // allocate new memory buffers
        var mapBuffer = _bufferFactory.AllocateNewBuffer();
        var masterBuffer = _bufferFactory.AllocateNewBuffer();

        var mapPage = new AllocationMapPage(AM_FIRST_PAGE_ID, mapBuffer);
        var masterPage = new DataPage(MASTER_PAGE_ID, MASTER_COL_ID, masterBuffer);

        // mark first extend to $master 
        mapBuffer.AsSpan(PAGE_HEADER_SIZE)[0] = MASTER_COL_ID;

        // create empty $master and writes on master data page
        _writer.WriteDocument(
            masterBuffer.AsSpan(PAGE_HEADER_SIZE), 
            MasterService.CreateNewMaster(), 
            out _);

        // update header pages
        mapPage.UpdateHeaderBuffer();
        masterPage.UpdateHeaderBuffer();

        // write both pages in disk and flush to OS
        await stream.WritePageAsync(mapBuffer);
        await stream.WritePageAsync(masterBuffer);
        await stream.FlushAsync();

        // deallocate buffers
        _bufferFactory.DeallocateBuffer(mapBuffer);
        _bufferFactory.DeallocateBuffer(masterBuffer);

        return fileHeader;
    }
}