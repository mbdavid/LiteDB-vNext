namespace LiteDB.Engine;

[AutoInterface]
unsafe internal partial struct PageMemory // PageMemory.DataBlock
{
    public static void InitializeAsDataPage(PageMemory* page, uint pageID, byte colID)
    {
        page->PageID = pageID;
        page->PageType = PageType.Data;
        page->ColID = colID;

        page->IsDirty = true;
    }

    public static DataBlockResult GetDataBlock(PageMemory* page, ushort index, out int dataBlockLength)
    {
        var segment = PageMemory.GetSegmentPtr(page, index);

        var dataBlock = (DataBlock*)((nint)page + segment->Location);

        dataBlockLength = segment->Length;

        var dataBlockID = new RowID(page->PageID, index);

        return new DataBlockResult(dataBlockID, page, segment, dataBlock);
    }

    public static DataBlockResult InsertDataBlock(PageMemory* page, Span<byte> content, bool extend, out bool defrag, out ExtendPageValue newPageValue)
    {
        // get required bytes this insert
        var bytesLength = (ushort)(content.Length + sizeof(DataBlock));

        // get a new index block
        var newIndex = PageMemory.GetFreeIndex(page);

        // get new rowid
        var dataBlockID = new RowID(page->PageID, newIndex);

        // get page segment for this data block
        var segment = PageMemory.InsertSegment(page, bytesLength, newIndex, true, out defrag, out newPageValue);

        var dataBlock = (DataBlock*)((nint)page + segment->Location);

        dataBlock->DataFormat = 0; // Bson
        dataBlock->Extend = extend;
        dataBlock->NextBlockID = RowID.Empty;

        var result = new DataBlockResult(dataBlockID, page, segment, dataBlock);

        // copy content into dataBlock content block
        content.CopyTo(result.AsSpan());

        return result;
    }


    /// <summary>
    /// Update an existing document inside a single page. This new document must fit on this page
    /// </summary>
    public static void UpdateDataBlock(PageMemory* page, ushort index, Span<byte> content, RowID nextBlock, out bool defrag, out ExtendPageValue newPageValue)
    {
        // get required bytes this update
        var bytesLength = (ushort)(content.Length + sizeof(DataBlock));

        page->IsDirty = true;

        // get page segment to update this buffer
        var segment = PageMemory.UpdateSegment(page, index, bytesLength, out defrag, out newPageValue);

        // get dataBlock pointer
        var dataBlock = (DataBlock*)((nint)page + segment->Location);

        dataBlock->DataFormat = 0; // Bson
        dataBlock->NextBlockID = nextBlock;

        // get datablock content pointer
        var contentPtr = (byte*)((nint)page + segment->Location + sizeof(DataBlock));

        // create a span and copy from source
        var dataBlockSpan = new Span<byte>(contentPtr, bytesLength);

        content.CopyTo(dataBlockSpan);
    }
}
