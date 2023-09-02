namespace LiteDB.Engine;

[AutoInterface]
unsafe internal class IndexPageService : PageService, IIndexPageService
{
    public void Initialize(PageMemory* pagePtr, uint pageID, byte colID)
    {
        pagePtr->PageID = pageID;
        pagePtr->PageType = PageType.Index;
        pagePtr->ColID = colID;

        pagePtr->IsDirty = true;
    }

    public InsertNodeResult InsertIndexNode(PageMemory* pagePtr, byte slot, byte levels, BsonValue key, RowID dataBlockID, ushort bytesLength)
    {
        // get a new index block
        var newIndex = base.GetFreeIndex(pagePtr);

        // get new rowid
        var indexNodeID = new RowID(pagePtr->PageID, newIndex);

        // get page segment for this indexNode
        var segmentPtr = base.Insert(pagePtr, bytesLength, newIndex, true);

        var indexNodePtr = (IndexNode*)&pagePtr->Buffer[segmentPtr->Location - PAGE_HEADER_SIZE];

        indexNodePtr->Slot = slot;
        indexNodePtr->Levels = levels;
        indexNodePtr->DataBlockID = dataBlockID;
        indexNodePtr->NextNodeID = RowID.Empty;

        // get first levels pointer
        var levelsOffset = segmentPtr->Location + sizeof(IndexNode);
        var levelsPtr = (IndexNodeLevel*)&pagePtr->Buffer[levelsOffset];

        // get index key pointer
        var keyOffset = levelsOffset + (levels * sizeof(IndexNodeLevel));
        var keyPtr = (IndexKey*)&pagePtr->Buffer[keyOffset];

        // get new indexKey and copy to memory
        var indexKey = new IndexKey(key);

        keyPtr->CopyFrom(indexKey);

        return new InsertNodeResult { IndexNodeID = indexNodeID, LevelsPtr = levelsPtr };
    }
}

