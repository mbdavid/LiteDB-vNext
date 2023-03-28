namespace LiteDB.Engine;

[AutoInterface]
internal class DataPageService : IDataPageService
{
    private readonly IBasePageService _pageService;

    public DataPageService(IBasePageService pageService)
    {
        _pageService = pageService;
    }

    public void CreateNew(PageBuffer buffer, uint pageID, byte colID)
    {
        buffer.Header.PageID = pageID;
        buffer.Header.PageType = PageType.Data;
        buffer.Header.ColID = colID;
    }

}
