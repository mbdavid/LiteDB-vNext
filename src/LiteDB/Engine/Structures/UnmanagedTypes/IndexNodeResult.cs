using System.Reflection.Emit;
using System.Xml.Linq;

namespace LiteDB.Engine;

unsafe internal struct IndexNodeResult
{
    public RowID IndexNodeID;
    public RowID DataBlockID;
    public PageMemory* Page;
    public IndexNode* Node;
    public IndexKey* Key;

    public static IndexNodeResult Empty = new() { IndexNodeID = RowID.Empty };

    public bool IsEmpty => this.IndexNodeID.IsEmpty;

    public IndexNodeResult(RowID indexNodeID, RowID dataBlockID, PageMemory* page, IndexNode* node, IndexKey* key)
    {
        this.IndexNodeID = indexNodeID;
        this.DataBlockID = dataBlockID;
        this.Page = page;
        this.Node = node;
        this.Key = key;
    }

    public IndexNodeLevel* this[int level]
    {
        get
        {
            ENSURE(level <= this.Node->Levels);

            var ptr = ((nint)Page + sizeof(IndexNode) + (level * sizeof(IndexNodeLevel)));

            return (IndexNodeLevel*)ptr;
        }
    }

    public void Deconstruct(out RowID indexNodeID, out PageMemory* pagePtr, out IndexNode* indexNodePtr, out IndexKey* indexKeyPtr)
    {
        indexNodeID = this.IndexNodeID;
        pagePtr = this.Page;
        indexNodePtr = this.Node;
        indexKeyPtr = this.Key;
    }

    public void Deconstruct(out IndexNode* node, out PageMemory* page)
    {
        page = this.Page;
        node = this.Node;
    }
}
