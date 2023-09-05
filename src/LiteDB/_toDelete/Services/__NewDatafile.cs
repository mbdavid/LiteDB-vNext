﻿namespace LiteDB.Engine;

[AutoInterface]
[Obsolete]
internal class __NewDatafile : I__NewDatafile
{
    private readonly IBufferFactory _bufferFactory;
    private readonly IMasterMapper _masterMapper;
    private readonly IBsonWriter _bsonWriter;
    private readonly I__DataPageService _dataPageService;
    private readonly IEngineSettings _settings;

    public __NewDatafile(
        IBufferFactory bufferFactory,
        IMasterMapper masterMapper,
        IBsonWriter bsonWriter,
        I__DataPageService dataPageService,
        IEngineSettings settings)
    {
        _bufferFactory = bufferFactory;
        _masterMapper = masterMapper;
        _bsonWriter = bsonWriter;
        _dataPageService =  dataPageService;
        _settings = settings;
    }

    /// <summary>
    /// Create a empty database using user-settings as default values
    /// Create FileHeader, first AllocationMap page and first $master data page
    /// </summary>
    public async ValueTask<FileHeader> CreateAsync(I__DiskStream writer)
    {
        // initialize FileHeader with user settings
        var fileHeader = new FileHeader(_settings);

        // create new file and write header
        await writer.CreateAsync(fileHeader);

        // create map page
        var mapPage = _bufferFactory.AllocateNewPage();

        mapPage.Header.PageID = __AM_FIRST_PAGE_ID;
        mapPage.Header.PageType = PageType.AllocationMap;

        // mark first extend to $master and first page as data
        mapPage.AsSpan(PAGE_HEADER_SIZE)[0] = MASTER_COL_ID;
        mapPage.AsSpan(PAGE_HEADER_SIZE)[1] = (byte)(1 << 6); // set first 3 bits as "01" - data page
        mapPage.IsDirty = true;

        // create $master page buffer
        var masterPage = _bufferFactory.AllocateNewPage();

        // initialize page buffer as data page
        _dataPageService.InitializeDataPage(masterPage, MASTER_PAGE_ID, MASTER_COL_ID);

        // create new/empty $master document
        var master = new MasterDocument();
        var masterDoc = _masterMapper.MapToDocument(master);
        using var masterBuffer = SharedBuffer.Rent(masterDoc.GetBytesCount());

        // serialize $master document 
        _bsonWriter.WriteDocument(masterBuffer.AsSpan(), masterDoc, out _);

        // insert $master document into master page
        _dataPageService.InsertDataBlock(masterPage, masterBuffer.AsSpan(), false);

        // initialize fixed position id 
        mapPage.PositionID = 0;
        masterPage.PositionID = 1;

        // write both pages in disk and flush to OS
        await writer.WritePageAsync(mapPage);
        await writer.WritePageAsync(masterPage);
        await writer.FlushAsync();

        // deallocate buffers
        _bufferFactory.DeallocatePage(mapPage);
        _bufferFactory.DeallocatePage(masterPage);

        return fileHeader;
    }
}