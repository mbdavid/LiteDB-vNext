using System;

namespace LiteDB.Engine;

[AutoInterface]
internal class DataPageService : PageService, IDataPageService
{
    /// <summary>
    /// Initialize an empty PageBuffer as DataPage
    /// </summary>
    public void InitializeDataPage(PageBuffer page, uint pageID, byte colID)
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
        // get required bytes this insert
        var bytesLength = (ushort)(buffer.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

        // get a new index block
        var newIndex = page.Header.GetFreeIndex(page);

        // get page segment for this data block
        var segment = base.Insert(page, bytesLength, newIndex, true);

        var rowID = new PageAddress(page.Header.PageID, newIndex);

        var dataBlock = new DataBlock(page, rowID, nextBlock);

        // get data block location inside page
        var dataBlockBuffer = page.AsSpan(segment.Location + DataBlock.P_BUFFER, buffer.Length);

        // copy content from span source to block right position 
        buffer.CopyTo(dataBlockBuffer);

        return dataBlock;
    }

    /// <summary>
    /// Update an existing document inside a single page. This new document must fit on this page
    /// </summary>
    public void UpdateDataBlock(PageBuffer page, byte index, Span<byte> buffer, PageAddress nextBlock)
    {
        // get required bytes this update
        var bytesLength = (ushort)(buffer.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

        // get page segment to update this buffer
        var segment = base.Update(page, index, bytesLength);

        // update nextBlock
        page.AsSpan(segment.Location + DataBlock.P_NEXT_BLOCK).WritePageAddress(nextBlock);

        // copy content from buffer to page
        buffer.CopyTo(page.AsSpan(segment.Location + DataBlock.DATA_BLOCK_FIXED_SIZE));
    }
}
