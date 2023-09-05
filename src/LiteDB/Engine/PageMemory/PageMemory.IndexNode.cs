namespace LiteDB.Engine;

unsafe internal partial struct PageMemory // PageMemory.IndexNode
{
    public static void InitializeAsIndexPage(PageMemory* page, uint pageID, byte colID)
    {
        page->PageID = pageID;
        page->PageType = PageType.Index;
        page->ColID = colID;

        page->IsDirty = true;
    }

    public static IndexNodeResult GetIndexNode(PageMemory* page, ushort index)
    {
        var segment = PageMemory.GetSegmentPtr(page, index);

        var indexNode = (IndexNode*)((nint)page + segment->Location);

        // get index key pointer
        var keyOffset = segment->Location + sizeof(IndexNode) + (indexNode->Levels * sizeof(IndexNodeLevel));
        var keyPtr = (IndexKey*)((nint)page + keyOffset);

        var indexNodeID = new RowID(page->PageID, index);

        return new IndexNodeResult(indexNodeID, indexNode->DataBlockID, page, segment, indexNode, keyPtr);
    }

    public static IndexNodeResult InsertIndexNode(PageMemory* page, byte slot, byte levels, IndexKey indexKey, RowID dataBlockID, out bool defrag, out ExtendPageValue newPageValue)
    {
        // get a new index block
        var newIndex = PageMemory.GetFreeIndex(page);

        // get new rowid
        var indexNodeID = new RowID(page->PageID, newIndex);

        var nodeLength = IndexNode.GetNodeLength(levels, indexKey);

        // get page segment for this indexNode
        var segment = PageMemory.InsertSegment(page, nodeLength, newIndex, true, out defrag, out newPageValue);

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

        return new IndexNodeResult(indexNodeID, dataBlockID, page, segment, indexNode, keyPtr);
    }
}

