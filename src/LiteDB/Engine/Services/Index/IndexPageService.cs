namespace LiteDB.Engine;

[AutoInterface]
internal class IndexPageService : IIndexPageService
{
    private readonly IPageService _pageService;

    public IndexPageService(IPageService pageService)
    {
        _pageService = pageService;
    }

    public void CreateNew(PageBuffer buffer, uint pageID, byte colID)
    {
        buffer.Header.PageID = pageID;
        buffer.Header.PageType = PageType.Index;
        buffer.Header.ColID = colID;
    }

}
