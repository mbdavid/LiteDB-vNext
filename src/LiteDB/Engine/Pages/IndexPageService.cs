using System;

namespace LiteDB.Engine;

[AutoInterface]
internal class IndexPageService : IIndexPageService
{
    private readonly IPageService _pageService;

    public IndexPageService(IPageService pageService)
    {
        _pageService = pageService;
    }

    public void CreateNew(PageBuffer page, uint pageID, byte colID)
    {
        page.Header.PageID = pageID;
        page.Header.PageType = PageType.Index;
        page.Header.ColID = colID;
    }

    public IndexNode GetIndexNode(PageBuffer page, byte index)
    {
        var segment = _pageService.Get(page, index, out var location);
        var position = new PageAddress(page.Header.PageID, index);

        var node = new IndexNode(segment, position, location);

        return node;
    }

    /// <summary>
    /// Insert new IndexNode. After call this, "node" instance can't be changed
    /// </summary>
    public IndexNode InsertIndexNode(PageBuffer page, byte slot, byte level, BsonValue key, PageAddress dataBlock, ushort bytesLength)
    {
        // get a new index block
        var index = page.Header.GetFreeIndex(page.Buffer);

        var position = new PageAddress(page.Header.PageID, index);

        // create new segment on page
        var segment = _pageService.Insert(page, bytesLength, index, true);

        // create a new index node
        var node = new IndexNode(position, segment, slot, level, key, dataBlock);

        return node;
    }

    /// <summary>
    /// Delete index node based on page index
    /// </summary>
    public void DeleteIndexNode(PageBuffer page, byte index)
    {
        _pageService.Delete(page, index);
    }
}
