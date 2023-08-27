namespace LiteDB.Engine;

[AutoInterface]
internal class IndexPageService : PageService, IIndexPageService
{
    /// <summary>
    /// Initialize an empty PageBuffer as IndexPage
    /// </summary>
    public void InitializeIndexPage(PageBuffer page, int pageID, byte colID)
    {
        page.Header.PageID = pageID;
        page.Header.PageType = PageType.Index;
        page.Header.ColID = colID;

        page.IsDirty = true;
    }

    /// <summary>
    /// Insert new IndexNode into an index page
    /// </summary>
    public IndexNode InsertIndexNode(PageBuffer page, byte slot, int levels, BsonValue key, PageAddress dataBlock, ushort bytesLength)
    {
        // get a new index block
        var index = page.Header.GetFreeIndex(page);

        var indexNodeID = new PageAddress(page.Header.PageID, index);

        // create new segment on page
        base.Insert(page, bytesLength, index, true);

        // create a new index node
        var node = new IndexNode(page, indexNodeID, slot, levels, key, dataBlock);

        return node;
    }

    /// <summary>
    /// Delete index node based on page index
    /// </summary>
    public void DeleteIndexNode(PageBuffer page, byte index)
    {
        base.Delete(page, index);
    }
}
