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

    public DataBlock* GetDataBlock(PageMemory* pagePtr, ushort index, out int dataBlockLength)
    {
        var segmentPtr = PageSegment.GetSegment(pagePtr, index);

        var dataBlock = (DataBlock*)(pagePtr + segmentPtr->Location);

        dataBlockLength = segmentPtr->Length;

        return dataBlock;
    }

    public DataBlock* InsertDataBlock(PageMemory* pagePtr, Span<byte> content, bool extend, out RowID dataBlockID)
    {
        // get required bytes this insert
        var bytesLength = (ushort)(content.Length + sizeof(DataBlock));

        // get a new index block
        var newIndex = base.GetFreeIndex(pagePtr);

        // get new rowid
        dataBlockID = new RowID(pagePtr->PageID, newIndex);

        // get page segment for this data block
        var segmentPtr = base.Insert(pagePtr, bytesLength, newIndex, true);

        var dataBlockPtr = (DataBlock*)&pagePtr->Buffer[segmentPtr->Location - PAGE_HEADER_SIZE];

        dataBlockPtr->DataFormat = 0; // Bson
        dataBlockPtr->Extend = extend;
        dataBlockPtr->NextBlockID = RowID.Empty;

        // get datablock content pointer
        var contentPtr = (byte*)((nint)pagePtr + segmentPtr->Location + sizeof(DataBlock));
        
        // create a span 
        var dataBlockSpan = new Span<byte>(contentPtr, bytesLength);

        content.CopyTo(dataBlockSpan);

        return dataBlockPtr;
    }


    /// <summary>
    /// Update an existing document inside a single page. This new document must fit on this page
    /// </summary>
    public void UpdateDataBlock(PageMemory* pagePtr, ushort index, Span<byte> content, RowID nextBlock)
    {
        // get required bytes this update
        var bytesLength = (ushort)(content.Length + sizeof(DataBlock));

        pagePtr->IsDirty = true;

        // get page segment to update this buffer
        var segmentPtr = base.Update(pagePtr, index, bytesLength);

        // get dataBlock pointer
        var dataBlockPtr = (DataBlock*)&pagePtr->Buffer[segmentPtr->Location - PAGE_HEADER_SIZE];

        dataBlockPtr->DataFormat = 0; // Bson
        dataBlockPtr->NextBlockID = nextBlock;

        // get datablock content pointer
        var contentPtr = (byte*)((nint)pagePtr + segmentPtr->Location + sizeof(DataBlock));

        // create a span and copy from source
        var dataBlockSpan = new Span<byte>(contentPtr, bytesLength);

        content.CopyTo(dataBlockSpan);
    }

    public void DeleteDataBlock(PageMemory* pagePtr, ushort index) => base.Delete(pagePtr, index);

}
