using System;

namespace LiteDB.Engine;

[AutoInterface]
internal class DataPageService : IDataPageService
{
    private readonly IBasePageService _pageService;

    public DataPageService(IBasePageService pageService)
    {
        _pageService = pageService;
    }

    /// <summary>
    /// Initialize an empty PageBuffer as DataPage
    /// </summary>
    public void CreateNew(PageBuffer buffer, uint pageID, byte colID)
    {
        buffer.Header.PageID = pageID;
        buffer.Header.PageType = PageType.Data;
        buffer.Header.ColID = colID;
    }

    /// <summary>
    /// Write a new document (or document fragment) into a DataPage
    /// </summary>
    public DataBlock InsertDataBlock(PageBuffer buffer, Span<byte> span, bool extend)
    {
        // get required bytes this update
        var bytesLength = (ushort)(span.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

        // get block from PageBlock
        var block = _pageService.Insert(buffer, bytesLength, out var index);

        var rowID = new PageAddress(buffer.Header.PageID, index);

        var dataBlock = new DataBlock(span, rowID, extend, PageAddress.Empty);

        // copy content from span source to block right position 
        span.CopyTo(block[DataBlock.P_BUFFER..block.Length]);

        return dataBlock;

    }

    /// <summary>
    /// Update data block content with new span buffer changes
    /// </summary>
    public void UpdateDataBlock(PageBuffer buffer, byte index, Span<byte> span)
    {
        // get required bytes this update
        var bytesLength = (ushort)(span.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

        var block = _pageService.Update(buffer, index, bytesLength);

        // copy content from span source to block right position 
        span.CopyTo(block[DataBlock.P_BUFFER..block.Length]);
    }
}
