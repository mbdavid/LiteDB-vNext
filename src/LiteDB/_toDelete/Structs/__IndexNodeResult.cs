namespace LiteDB.Engine;

/// <summary>
/// Transport strcut
/// </summary>
[Obsolete]
internal readonly struct __IndexNodeResult
{
    public static readonly __IndexNodeResult Empty = new();

    private readonly __IndexNode _node;
    private readonly PageBuffer? _page;

    public __IndexNode Node => _node;

    public PageBuffer Page => _page!;

    public bool IsEmpty => _node.IsEmpty;

    public __IndexNodeResult()
    {
        _node = __IndexNode.Empty;
        _page = null;
    }

    public __IndexNodeResult(__IndexNode node, PageBuffer page)
    {
        _node = node;
        _page = page;
    }

    public void Deconstruct(out __IndexNode node, out PageBuffer page)
    {
        node = _node;
        page = _page!;
    }

    public override string ToString()
    {
        return IsEmpty ? "<EMPTY>" : Dump.Object(new { PageID = Dump.PageID(Page.Header.PageID), PositionID = Dump.PageID(Page.PositionID), Node });
    }
}
