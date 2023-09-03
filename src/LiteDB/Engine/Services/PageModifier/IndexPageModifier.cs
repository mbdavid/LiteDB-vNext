using System;

namespace LiteDB.Engine;

[AutoInterface]
unsafe internal class IndexPageModifier : BasePageModifier, IIndexPageModifier
{
    public void Initialize(PageMemory* pagePtr, uint pageID, byte colID)
    {
        pagePtr->PageID = pageID;
        pagePtr->PageType = PageType.Index;
        pagePtr->ColID = colID;

        pagePtr->IsDirty = true;
    }

    public IndexNodeResult GetIndexNode(PageMemory* pagePtr, ushort index)
    {
        var segmentPtr = PageSegment.GetSegment(pagePtr, index);

        var indexNodePtr = (IndexNode*)(pagePtr + segmentPtr->Location);

        // get first levels pointer
        var levelsOffset = segmentPtr->Location + sizeof(IndexNode);
        var levelsPtr = (IndexNodeLevel*)&pagePtr->Buffer[levelsOffset];

        // get index key pointer
        var keyOffset = levelsOffset + (indexNodePtr->Levels * sizeof(IndexNodeLevel));
        var keyPtr = (IndexKey*)&pagePtr->Buffer[keyOffset];

        var indexNodeID = new RowID(pagePtr->PageID, index);

        return new IndexNodeResult(indexNodeID, indexNodePtr->DataBlockID, pagePtr, indexNodePtr, levelsPtr, keyPtr);
    }

    public IndexNodeResult InsertIndexNode(PageMemory* pagePtr, byte slot, byte levels, IndexKey indexKey, RowID dataBlockID, ushort bytesLength)
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
        keyPtr->CopyFrom(indexKey);

        return new IndexNodeResult(indexNodeID, dataBlockID, pagePtr, indexNodePtr, levelsPtr, keyPtr);
    }

    public void DeleteIndexNode(PageMemory* pagePtr, ushort index) => base.Delete(pagePtr, index);
}

