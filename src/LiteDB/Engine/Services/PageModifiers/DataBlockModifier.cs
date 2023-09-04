using System;

namespace LiteDB.Engine;

[AutoInterface]
unsafe internal partial struct PageMemory
{
    public void InitializeAsDataPage(uint pageID, byte colID)
    {
        this.PageID = pageID;
        this.PageType = PageType.Data;
        this.ColID = colID;

        this.IsDirty = true;
    }

    public DataBlockResult GetDataBlock(ushort index, out int dataBlockLength)
    {
        fixed (PageMemory* page = &this)
        {
            var segment = this.GetSegmentPtr(index);

            var dataBlock = (DataBlock*)((nint)page + segment->Location);

            dataBlockLength = segment->Length;

            var dataBlockID = new RowID(page->PageID, index);

            return new DataBlockResult(dataBlockID, page, segment, dataBlock);
        }
    }

    public DataBlockResult InsertDataBlock(Span<byte> content, bool extend, out bool defrag, out ExtendPageValue newPageValue)
    {
        fixed (PageMemory* page = &this)
        {
            // get required bytes this insert
            var bytesLength = (ushort)(content.Length + sizeof(DataBlock));

            // get a new index block
            var newIndex = this.GetFreeIndex();

            // get new rowid
            var dataBlockID = new RowID(this.PageID, newIndex);

            // get page segment for this data block
            var segment = this.InsertSegment(bytesLength, newIndex, true, out defrag);

            var dataBlock = (DataBlock*)((nint)page + segment->Location);

            dataBlock->DataFormat = 0; // Bson
            dataBlock->Extend = extend;
            dataBlock->NextBlockID = RowID.Empty;

            var result = new DataBlockResult(dataBlockID, page, segment, dataBlock);

            // copy content into dataBlock content block
            content.CopyTo(result.AsSpan());

            return result;
        }
    }


    /// <summary>
    /// Update an existing document inside a single page. This new document must fit on this page
    /// </summary>
    public void UpdateDataBlock(ushort index, Span<byte> content, RowID nextBlock, out bool defrag)
    {
        fixed (PageMemory* page = &this)
        {
            // get required bytes this update
            var bytesLength = (ushort)(content.Length + sizeof(DataBlock));

            this.IsDirty = true;

            // get page segment to update this buffer
            var segment = this.UpdateSegment(index, bytesLength, out defrag);

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
}
