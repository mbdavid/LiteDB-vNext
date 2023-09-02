namespace LiteDB.Engine;

[AutoInterface]
internal class NewDatafile : INewDatafile
{
    private readonly IMemoryFactory _memoryFactory;
    private readonly IMasterMapper _masterMapper;
    private readonly IBsonWriter _bsonWriter;
    private readonly I__DataPageService _dataPageService;
    private readonly IEngineSettings _settings;

    public NewDatafile(
        IMemoryFactory memoryFactory,
        IMasterMapper masterMapper,
        IBsonWriter bsonWriter,
        I__DataPageService dataPageService,
        IEngineSettings settings)
    {
        _memoryFactory = memoryFactory;
        _masterMapper = masterMapper;
        _bsonWriter = bsonWriter;
        _dataPageService = dataPageService;
        _settings = settings;
    }

    /// <summary>
    /// Create a empty database using user-settings as default values
    /// Create FileHeader, first AllocationMap page and first $master data page
    /// </summary>
    public async ValueTask<FileHeader> CreateNewAsync(IDiskStream writer)
    {
        // initialize FileHeader with user settings
        var fileHeader = new FileHeader(_settings);

        // create new file and write header
        writer.OpenFile(fileHeader);

        unsafe
        {
            // create map page
            var mapPagePtr = _memoryFactory.AllocateNewPage();

            mapPagePtr->PageID = AM_FIRST_PAGE_ID;
            mapPagePtr->PageType = PageType.AllocationMap;

            // mark first extend to $master and first page as data
            mapPagePtr->Buffer[0] = MASTER_COL_ID;
            mapPagePtr->Buffer[1] = (byte)(1 << 6); // set first 3 bits as "001" - data page

            mapPagePtr->IsDirty = true;

            // create $master page buffer
            var masterPagePtr = _memoryFactory.AllocateNewPage();

            // initialize page buffer as data page
            // _dataPageService.InitializeDataPage(masterPagePtr, MASTER_PAGE_ID, MASTER_COL_ID);

            // create new/empty $master document
            var master = new MasterDocument();
            var masterDoc = _masterMapper.MapToDocument(master);
            using var masterBuffer = SharedBuffer.Rent(masterDoc.GetBytesCount());

            // serialize $master document 
            _bsonWriter.WriteDocument(masterBuffer.AsSpan(), masterDoc, out _);

            // insert $master document into master page
            // _dataPageService.InsertDataBlock(masterPagePtr, masterBuffer.AsSpan(), false);

            // initialize fixed position id 
            mapPagePtr->PositionID = 0;
            masterPagePtr->PositionID = 1;

            // write both pages in disk and flush to OS
            writer.WritePage(mapPagePtr);
            writer.WritePage(masterPagePtr);

            // deallocate buffers
            _memoryFactory.DeallocatePage(mapPagePtr);
            _memoryFactory.DeallocatePage(masterPagePtr);
        }

        await writer.FlushAsync();

        return fileHeader;
    }
}
