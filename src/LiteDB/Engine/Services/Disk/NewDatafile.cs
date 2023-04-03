namespace LiteDB.Engine;

[AutoInterface]
internal class NewDatafile : INewDatafile
{
    private readonly IServicesFactory _factory;
    private readonly IBufferFactoryService _bufferFactory;
    private readonly IBsonWriter _writer;
    private readonly IDataPageService _dataPageService;
    private readonly IEngineSettings _settings;

    public NewDatafile(IServicesFactory factory)
    {
        _factory = factory;
        _bufferFactory = factory.GetBufferFactory();
        _writer = _factory.GetBsonWriter();
        _dataPageService =  _factory.GetDataPageService();
        _settings = factory.Settings;
    }

    /// <summary>
    /// Create a empty database using user-settings as default values
    /// Create FileHeader, first AllocationMap page and first $master data page
    /// </summary>
    public async Task<FileHeader> CreateAsync(IFileDiskStream stream)
    {
        // initialize FileHeader with user settings
        var fileHeader = new FileHeader(_settings);

        // create new file and write header
        await stream.CreateAsync(fileHeader);

        // create map page
        var mapBuffer = _bufferFactory.AllocateNewBuffer();

        mapBuffer.Header.PageID = AM_FIRST_PAGE_ID;
        mapBuffer.Header.PageType = PageType.AllocationMap;

        // mark first extend to $master 
        mapBuffer.AsSpan(PAGE_HEADER_SIZE)[0] = MASTER_COL_ID;

        // create $master page
        var masterBuffer = _bufferFactory.AllocateNewBuffer();

        // create new data page
        _dataPageService.CreateNew(masterBuffer, MASTER_PAGE_ID, MASTER_COL_ID);

        // create empty $master and writes on master data page
        _writer.WriteDocument(
            masterBuffer.AsSpan(PAGE_HEADER_SIZE), 
            MasterService.CreateNewMaster(), 
            out _);

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