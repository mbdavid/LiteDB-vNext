using System.IO.Pipes;

namespace LiteDB.Engine;

[AutoInterface]
unsafe internal class DataPageModifier : BasePageModifier, IDataPageModifier
{
    public void Initialize(PageMemory* pagePtr, uint pageID, byte colID)
    {
        pagePtr->PageID = pageID;
        pagePtr->PageType = PageType.Data;
        pagePtr->ColID = colID;

        pagePtr->IsDirty = true;
    }

    public DataBlock* GetDataBlock(PageMemory* page, ushort index, out int dataBlockLength)
    {
        var segmentPtr = PageSegment.GetSegment(page, index);

        var dataBlock = (DataBlock*)((nint)page + segmentPtr->Location);

        dataBlockLength = segmentPtr->Length;

        return dataBlock;
    }

    public DataBlock* InsertDataBlock(PageMemory* page, Span<byte> content, bool extend, out RowID dataBlockID)
    {
        // get required bytes this insert
        var bytesLength = (ushort)(content.Length + sizeof(DataBlock));

        // get a new index block
        var newIndex = base.GetFreeIndex(page);

        // get new rowid
        dataBlockID = new RowID(page->PageID, newIndex);

        // get page segment for this data block
        var segment = base.Insert(page, bytesLength, newIndex, true);

        var dataBlock = (DataBlock*)((nint)page + segment->Location);

        dataBlock->DataFormat = 0; // Bson
        dataBlock->Extend = extend;
        dataBlock->NextBlockID = RowID.Empty;

        // get datablock content pointer
        var contentPtr = (byte*)((nint)page + segment->Location + sizeof(DataBlock));
        
        // create a span 
        var contentSpan = new Span<byte>(contentPtr, bytesLength);

        content.CopyTo(contentSpan);

        return dataBlock;
    }


    /// <summary>
    /// Update an existing document inside a single page. This new document must fit on this page
    /// </summary>
    public void UpdateDataBlock(PageMemory* page, ushort index, Span<byte> content, RowID nextBlock)
    {
        // get required bytes this update
        var bytesLength = (ushort)(content.Length + sizeof(DataBlock));

        page->IsDirty = true;

        // get page segment to update this buffer
        var segmentPtr = base.Update(page, index, bytesLength);

        // get dataBlock pointer
        var dataBlockPtr = (DataBlock*)&page->Buffer[segmentPtr->Location - PAGE_HEADER_SIZE];

        dataBlockPtr->DataFormat = 0; // Bson
        dataBlockPtr->NextBlockID = nextBlock;

        // get datablock content pointer
        var contentPtr = (byte*)((nint)page + segmentPtr->Location + sizeof(DataBlock));

        // create a span and copy from source
        var dataBlockSpan = new Span<byte>(contentPtr, bytesLength);

        content.CopyTo(dataBlockSpan);
    }

    public void DeleteDataBlock(PageMemory* page, ushort index) => base.Delete(page, index);

}
