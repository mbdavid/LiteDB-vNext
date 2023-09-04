namespace LiteDB.Engine;

unsafe internal partial struct PageMemory
{
    public void InitializeAsIndexPage(uint pageID, byte colID)
    {
        this.PageID = pageID;
        this.PageType = PageType.Index;
        this.ColID = colID;

        this.IsDirty = true;
    }

    public IndexNodeResult GetIndexNode(ushort index)
    {
        fixed(PageMemory* page = &this)
        {
            var segment = this.GetSegmentPtr(index);

            var indexNode = (IndexNode*)((nint)page + segment->Location);

            // get index key pointer
            var keyOffset = segment->Location + sizeof(IndexNode) + (indexNode->Levels * sizeof(IndexNodeLevel));
            var keyPtr = (IndexKey*)((nint)page + keyOffset);

            var indexNodeID = new RowID(page->PageID, index);

            return new IndexNodeResult(indexNodeID, indexNode->DataBlockID, page, indexNode, keyPtr);
        }
    }

    public IndexNodeResult InsertIndexNode(byte slot, byte levels, IndexKey indexKey, RowID dataBlockID, ushort bytesLength)
    {
        fixed (PageMemory* page = &this)
        {
            // get a new index block
            var newIndex = this.GetFreeIndex();

            // get new rowid
            var indexNodeID = new RowID(page->PageID, newIndex);

            // get page segment for this indexNode
            var segment = this.InsertSegment(bytesLength, newIndex, true);

            var indexNode = (IndexNode*)((nint)page + segment->Location);

            // initialize indexNode
            indexNode->Slot = slot;
            indexNode->Levels = levels;
            indexNode->DataBlockID = dataBlockID;
            indexNode->NextNodeID = RowID.Empty;

            var levelPtr = (IndexNodeLevel*)((nint)indexNode + sizeof(IndexNode));

            for (var l = 0; l < levels; l++)
            {
                levelPtr->NextID = levelPtr->PrevID = RowID.Empty;
                levelPtr++;
            }

            // after write all level nodes, levelPtr are at IndexKey location
            var keyPtr = (IndexKey*)levelPtr;

            // get new indexKey and copy to memory
            IndexKey.CopyValues(indexKey, keyPtr);

            return new IndexNodeResult(indexNodeID, dataBlockID, page, indexNode, keyPtr);
        }
    }
}

