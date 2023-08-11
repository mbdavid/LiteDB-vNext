namespace LiteDB.Engine;

/// <summary>
/// Transport strcut
/// </summary>
internal readonly struct IndexNodeRef
{
    public readonly IndexNode Node;
    public readonly PageBuffer Page;

    public IndexNodeRef (IndexNode node, PageBuffer page)
    {
        this.Node = node;
        this.Page = page;
    }

    public override string ToString()
    {
        return $"{{ PageID = {Page.Header.PageID}, PositionID = {Page.PositionID}, Node = {Node} }}";
    }
}
