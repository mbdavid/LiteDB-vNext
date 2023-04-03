using System;

namespace LiteDB.Engine;

[AutoInterface]
internal class DataPageService : IDataPageService
{
    private readonly IPageService _pageService;

    public DataPageService(IPageService pageService)
    {
        _pageService = pageService;
    }

    /// <summary>
    /// Initialize an empty PageBuffer as DataPage
    /// </summary>
    public void CreateNew(PageBuffer page, uint pageID, byte colID)
    {
        page.Header.PageID = pageID;
        page.Header.PageType = PageType.Data;
        page.Header.ColID = colID;
    }

    /// <summary>
    /// Write a new document (or document fragment) into a DataPage
    /// </summary>
    public DataBlock InsertDataBlock(PageBuffer page, Span<byte> span, bool extend)
    {
        // get required bytes this update
        var bytesLength = (ushort)(span.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

        // get block from PageBlock
        var block = _pageService.Insert(page, bytesLength, out var index);

        var rowID = new PageAddress(page.Header.PageID, index);

        var dataBlock = new DataBlock(span, rowID, extend, PageAddress.Empty);

        // copy content from span source to block right position 
        span.CopyTo(block[DataBlock.P_BUFFER..block.Length]);

        return dataBlock;
    }
}
