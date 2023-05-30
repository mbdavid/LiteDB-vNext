using System;

namespace LiteDB.Engine;

[AutoInterface]
internal class DataPageService : PageService, IDataPageService
{
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
    public DataBlock InsertDataBlock(PageBuffer page, Span<byte> buffer, PageAddress nextBlock)
    {
        // get required bytes this update
        var bytesLength = (ushort)(buffer.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

        // get a new index block
        var newIndex = page.Header.GetFreeIndex(page);

        // get page segment for this data block
        var segment = base.Insert(page, bytesLength, newIndex, true);

        var rowID = new PageAddress(page.Header.PageID, newIndex);

        var dataBlock = new DataBlock(buffer, rowID, nextBlock);

        // copy content from span source to block right position 
        buffer.CopyTo(segment[DataBlock.P_BUFFER..segment.Length]);

        return dataBlock;
    }

    public void UpdateNextBlock(PageBuffer page, byte index, PageAddress nextBlock)
    {
        var span = _pageService.Get(page, index, false);

        span[DataBlock.P_NEXT_BLOCK..].WritePageAddress(nextBlock);
    }
}
