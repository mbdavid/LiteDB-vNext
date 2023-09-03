using System.Reflection.Emit;
using System.Xml.Linq;

namespace LiteDB.Engine;

unsafe internal struct IndexNodeResult
{
    public RowID IndexNodeID;
    public PageMemory* Page;
    public IndexNode* Node;
    public IndexNodeLevel* Levels;
    public IndexKey* Key;

    public static IndexNodeResult Empty = new() { IndexNodeID = RowID.Empty };

    public bool IsEmpty => this.IndexNodeID.IsEmpty;

    public IndexNodeResult(RowID indexNodeID, PageMemory* page, IndexNode* node, IndexNodeLevel* levels, IndexKey* key)
    {
        this.IndexNodeID = indexNodeID;
        this.Page = page;
        this.Node = node;
        this.Levels = levels;
        this.Key = key;
    }

    public IndexNodeLevel* this[int level]
    {
        get
        {
            ENSURE(level <= this.Node->Levels);

            var ptr = this.Levels + (level * sizeof(IndexNodeLevel));

            return ptr;
        }
    }

    public void Deconstruct(out RowID indexNodeID, out PageMemory* pagePtr, out IndexNode* indexNodePtr, out IndexNodeLevel* levelsPtr, out IndexKey* indexKeyPtr)
    {
        indexNodeID = this.IndexNodeID;
        pagePtr = this.Page;
        indexNodePtr = this.Node;
        levelsPtr = this.Levels;
        indexKeyPtr = this.Key;
    }

    public void Deconstruct(out IndexNode* node, out PageMemory* page)
    {
        page = this.Page;
        node = this.Node;
    }
}
