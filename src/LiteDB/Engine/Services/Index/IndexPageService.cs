namespace LiteDB.Engine;

[AutoInterface]
internal class IndexPageService : IIndexPageService
{
    private readonly IBasePageService _pageService;

    public IndexPageService(IBasePageService pageService)
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
