namespace LiteDB.Engine;

/// <summary>
/// Implement a Index service - Add/Remove index nodes on SkipList
/// Based on: http://igoro.com/archive/skip-lists-are-fascinating/
/// </summary>
[AutoInterface]
internal class IndexService : IIndexService
{
    //TODO: dummy test for cache!!
    private static ConcurrentDictionary<PageAddress, IndexNode> _globalCache = new ();  // PageAddress based on Position (after log)


    // dependency injection
    private readonly IIndexPageService _indexPageService;
    private readonly ITransaction _transaction;
    private readonly Collation _collation;

    public IndexService(
        IIndexPageService indexPageService,
        Collation collation,
        ITransaction transaction)
    {
        _indexPageService = indexPageService;
        _collation = collation;
        _transaction = transaction;
    }

    /// <summary>
    /// Create head and tail nodes for a new index
    /// </summary>
    public async ValueTask<(IndexNode head, IndexNode tail)> CreateHeadTailNodesAsync(byte colID)
    {
        // get how many bytes needed for each head/tail (both has same size)
        var bytesLength = (ushort)IndexNode.GetNodeLength(INDEX_MAX_LEVELS, BsonValue.MinValue, out _);

        // get a index page for this collection
        var page = await _transaction.GetFreeIndexPageAsync(colID, bytesLength);

        // get initial pageExtend value
        var before = page.Header.ExtendPageValue;

        // add head/tail nodes into page
        var head = _indexPageService.InsertIndexNode(page, 0, INDEX_MAX_LEVELS, BsonValue.MinValue, PageAddress.Empty, bytesLength);
        var tail = _indexPageService.InsertIndexNode(page, 0, INDEX_MAX_LEVELS, BsonValue.MaxValue, PageAddress.Empty, bytesLength);

        // link head-to-tail with double link list in first level
        head.SetNext(page, 0, tail.IndexNodeID);
        tail.SetPrev(page, 0, head.IndexNodeID);

        // update allocation map if needed
        var after = page.Header.ExtendPageValue;

        if (before != after)
        {
            _transaction.UpdatePageMap(page.Header.PageID, after);
        }

        return (head, tail);
    }

    /// <summary>
    /// Insert a new node index inside an collection index. Flip coin to know level
    /// </summary>
    public async ValueTask<IndexNodeResult> AddNodeAsync(byte colID, IndexDocument index, BsonValue key, PageAddress dataBlock, IndexNodeResult last)
    {
        // do not accept Min/Max value as index key (only head/tail can have this value)
        if (key.IsMaxValue || key.IsMinValue) throw ERR($"BsonValue MaxValue/MinValue are not supported as index key");

        // random level (flip coin mode) - return number between 0-31
        var levels = this.Flip();

        // call AddNode with key value
        return await this.AddNodeAsync(colID, index, key, dataBlock, levels, last);
    }

    /// <summary>
    /// Insert a new node index inside an collection index.
    /// </summary>
    private async ValueTask<IndexNodeResult> AddNodeAsync(byte colID, IndexDocument index, BsonValue key, PageAddress dataBlock, int insertLevels, IndexNodeResult last)
    {
        // get a free index page for head note
        var bytesLength = (ushort)IndexNode.GetNodeLength(insertLevels, key, out var keyLength);

        // test for index key maxlength
        if (keyLength > INDEX_MAX_KEY_LENGTH) throw ERR($"Index key must be less than {INDEX_MAX_KEY_LENGTH} bytes.");

        // get an index page with avaliable space to add this node
        var page = await _transaction.GetFreeIndexPageAsync(colID, bytesLength);

        // get initial pageValue
        var before = page.Header.ExtendPageValue;

        // create node in buffer
        var node = _indexPageService.InsertIndexNode(page, index.Slot, insertLevels, key, dataBlock, bytesLength);

        // update allocation map if needed (this page has no more "size" changes)
        var after = page.Header.ExtendPageValue;

        if (before != after)
        {
            _transaction.UpdatePageMap(page.Header.PageID, after);
        }

        // now, let's link my index node on right place
        var left = index.HeadIndexNodeID;
        var leftNode = await this.GetNodeAsync(left);

        // for: scan from top to bottom
        for (int i = INDEX_MAX_LEVELS - 1; i >= 0; i--)
        {
            var currentLevel = (byte)i;
            var right = leftNode.Node.Next[currentLevel];

            // while: scan from left to right
            while (right.IsEmpty == false)
            {
                var rightNode = await this.GetNodeAsync(right);

                // read next node to compare
                var diff = rightNode.Node.Key.CompareTo(key, _collation);

                // if unique and diff == 0, throw index exception (must rollback transaction - others nodes can be dirty)
                if (diff == 0 && index.Unique) throw ERR("IndexDuplicateKey(index.Name, key)");

                if (diff == 1) break; // stop going right

                leftNode = rightNode;
                right = rightNode.Node.Next[currentLevel];
            }

            if (currentLevel <= insertLevels - 1) // level == length
            {
                // prev: immediately before new node
                // node: new inserted node
                // next: right node from prev (where left is pointing)

                var prev = leftNode.Node.IndexNodeID;
                var next = leftNode.Node.Next[currentLevel];

                // if next is empty, use tail (last key)
                if (next.IsEmpty) next = index.TailIndexNodeID;

                // set new node pointer links with current level sibling
                node.SetNext(page, currentLevel, next);
                node.SetPrev(page, currentLevel, prev);
                
                // fix sibling pointer to new node
                leftNode.Node.SetNext(leftNode.Page, currentLevel, node.IndexNodeID);

                right = node.Next[currentLevel];

                var rightNode = await this.GetNodeAsync(right);
                rightNode.Node.SetPrev(rightNode.Page, currentLevel, node.IndexNodeID);
            }
        }

        // if last node exists, create a single link list between node list
        if (!last.IsEmpty)
        {
            // set last node to link with current node
            last.Node.SetNextNodeID(last.Page, node.IndexNodeID);
        }

        return new (node, page);
    }

    /// <summary>
    /// Flip coin (skipped list): returns how many levels the node will have (starts in 1, max of INDEX_MAX_LEVELS)
    /// </summary>
    public int Flip()
    {
        byte levels = 1;

        for (int R = Randomizer.Next(); (R & 1) == 1; R >>= 1)
        {
            levels++;
            if (levels == INDEX_MAX_LEVELS) break;
        }

        return levels;
    }

    /// <summary>
    /// Get a node/pageBuffer inside a page using PageAddress. IndexNodeID must be a valid position
    /// </summary>
    public async ValueTask<IndexNodeResult> GetNodeAsync(PageAddress indexNodeID)
    {
        var page = await _transaction.GetPageAsync(indexNodeID.PageID);

        ENSURE(page.Header.PageType == PageType.Index, new { indexNodeID, page });

        var indexNode = _transaction.GetIndexNode(indexNodeID);

        return new(indexNode, page);
    }

    #region Find

    /// <summary>
    /// Find first node that index match with value . 
    /// If index are unique, return unique value - if index are not unique, return first found (can start, middle or end)
    /// If not found but sibling = true and key are not found, returns next value index node (if order = Asc) or prev node (if order = Desc)
    /// </summary>
    public async ValueTask<IndexNodeResult> FindAsync(IndexDocument index, BsonValue key, bool sibling, int order)
    {
        var left = order == Query.Ascending ? index.HeadIndexNodeID : index.TailIndexNodeID;
        var leftNode = await this.GetNodeAsync(left);

        for (var level = INDEX_MAX_LEVELS - 1; level >= 0; level--)
        {
            var right = leftNode.Node.GetNextPrev(level, order);

            while (right.IsEmpty == false)
            {
                var rightNode = await this.GetNodeAsync(right);

                var diff = rightNode.Node.Key.CompareTo(key, _collation);
                
                if (diff == order && (level > 0 || !sibling)) break; // go down one level

                if (diff == order && level == 0 && sibling)
                {
                    // is head/tail?
                    return (rightNode.Node.Key.IsMinValue || rightNode.Node.Key.IsMaxValue) ? IndexNodeResult.Empty : rightNode;
                }

                // if equals, return index node
                if (diff == 0)
                {
                    return rightNode;
                }

                leftNode = rightNode;
                right = rightNode.Node.GetNextPrev(level, order);
            }

        }

        return IndexNodeResult.Empty;
    }

    #endregion

    /// <summary>
    /// Deletes all indexes nodes from pkNode
    /// </summary>
    public async ValueTask DeleteAllAsync(IndexNodeResult nodeResult)
    {
        // all indexes nodes from a document are connected by nextNode
        while (!nodeResult.IsEmpty)
        {
            await this.DeleteSingleNodeAsync(nodeResult);

            if (nodeResult.Node.NextNodeID.IsEmpty) break;

            // move to next node
            nodeResult = await this.GetNodeAsync(nodeResult.Node.NextNodeID);
        }
    }

    /// <summary>
    /// Delete a single node fixing all next/prev levels pointers
    /// </summary>
    private async ValueTask DeleteSingleNodeAsync(IndexNodeResult nodeResult)
    {
        var (node, page) = nodeResult;

        // run over all levels linking prev with next
        for (int i = node.Levels - 1; i >= 0; i--)
        {
            // get previous and next nodes (between my deleted node)
            var (prevNode, prevPage) = await this.GetNodeAsync(node.Prev[i]);
            var (nextNode, nextPage) = await this.GetNodeAsync(node.Next[i]);

            if (!prevNode.IsEmpty)
            {
                prevNode.SetNext(prevPage, (byte)i, node.Next[i]);
            }

            if (!nextNode.IsEmpty)
            {
                nextNode.SetPrev(nextPage, (byte)i, node.Prev[i]);
            }
        }

        // get extend page value before page change
        var before = page.Header.ExtendPageValue;

        // delete node segment in page
        _indexPageService.DeleteIndexNode(page, node.IndexNodeID.Index);

        // delete index node reference from collection
        _transaction.DeleteIndexNode(node.IndexNodeID);

        // update map page only if change page value
        var after = page.Header.ExtendPageValue;

        if (before != after)
        {
            _transaction.UpdatePageMap(page.Header.PageID, after);
        }
    }
}