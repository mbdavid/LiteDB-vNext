namespace LiteDB.Engine;

/// <summary>
/// Transport strcut
/// </summary>
internal struct IndexNodeRef
{
    public IndexNode Node;
    public PageBuffer Page;

    public IndexNodeRef (IndexNode node, PageBuffer page)
    {
        this.Node = node;
        this.Page = page;
    }
}
