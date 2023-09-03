﻿namespace LiteDB.Engine;

[AutoInterface]
[Obsolete]
internal class __IndexPageService : __PageService, I__IndexPageService
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
    /// Insert new __IndexNode into an index page
    /// </summary>
    public __IndexNode InsertIndexNode(PageBuffer page, byte slot, int levels, BsonValue key, PageAddress dataBlock, ushort bytesLength)
    {
        // get a new index block
        var index = page.Header.GetFreeIndex(page);

        var indexNodeID = new PageAddress(page.Header.PageID, index);

        // create new segment on page
        base.Insert(page, bytesLength, index, true);

        // create a new index node
        var node = new __IndexNode(page, indexNodeID, slot, levels, key, dataBlock);

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
