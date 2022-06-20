namespace LiteDB.Engine;

/// <summary>
/// The IndexPage thats stores object data.
/// </summary>
internal class IndexPage : BlockPage
{
    /// <summary>
    /// Create new IndexPage
    /// </summary>
    public IndexPage(uint pageID, byte colID)
        : base(pageID, PageType.Index, colID)
    {
    }

    /// <summary>
    /// Load index page from buffer
    /// </summary>
    public IndexPage(IMemoryOwner<byte> buffer)
        : base(buffer)
    {
        ENSURE(this.PageType == PageType.Data, "page type must be index page");
    }

    /// <summary>
    /// Read single IndexNode
    /// </summary>
    public IndexNode GetIndexNode(byte index, bool readOnly)
    {
        var block = base.Get(index, readOnly);
        var position = new PageAddress(this.PageID, index);

        var node = new IndexNode(position, block);

        return node;
    }

    /// <summary>
    /// Insert new IndexNode on page
    /// </summary>
    public IndexNode InsertIndexNode(byte slot, byte level, BsonValue key, PageAddress dataBlock, int bytesLength)
    {
        var block = base.Insert((ushort)bytesLength, out var index);
        var position = new PageAddress(this.PageID, index);

        var node = new IndexNode(position, block, slot, level, key, dataBlock);

        return node;
    }

    /// <summary>
    /// Update Prev[index] pointer (update in buffer too). Also, set page as dirty
    /// </summary>
    public void SetPrev(IndexNode node, byte level, PageAddress prevValue)
    {
        //var block = base.Get(node., false);
        //
        //node.SetNext(level, prevValue, span);
    }

    /// <summary>
    /// Delete index node based on page index
    /// </summary>
    public void DeleteIndexNode(byte index)
    {
        base.Delete(index);
    }
}
