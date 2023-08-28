namespace LiteDB.Engine;

/// <summary>
/// Transport strcut
/// </summary>
internal readonly struct IndexNodeResult
{
    public static readonly IndexNodeResult Empty = new();

    private readonly IndexNode _node;
    private readonly PageBuffer? _page;

    public IndexNode Node => _node;

    public PageBuffer Page => _page!;

    public bool IsEmpty => _node.IsEmpty;

    public IndexNodeResult()
    {
        _node = IndexNode.Empty;
        _page = null;
    }

    public IndexNodeResult(IndexNode node, PageBuffer page)
    {
        _node = node;
        _page = page;
    }

    public void Deconstruct(out IndexNode node, out PageBuffer page)
    {
        node = _node;
        page = _page!;
    }

    public override string ToString()
    {
        return IsEmpty ? "<EMPTY>" : Dump.Object(new { PageID = Dump.PageID(Page.Header.PageID), PositionID = Dump.PageID(Page.PositionID), Node });
    }
}
