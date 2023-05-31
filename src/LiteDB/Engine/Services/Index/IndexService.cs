using System.Xml.Linq;

namespace LiteDB.Engine;

/// <summary>
/// Implement a Index service - Add/Remove index nodes on SkipList
/// Based on: http://igoro.com/archive/skip-lists-are-fascinating/
/// </summary>
[AutoInterface]
internal class IndexService : IIndexService
{
    // dependency injection
    private readonly IAllocationMapService _allocationMap;
    private readonly IIndexPageService _indexPage;
    private readonly IBsonWriter _writer;
    private readonly ITransaction _transaction;
    private readonly Collation _collation;
    private readonly Random _random; // do not use as static (Random are not thread safe)

    public IndexService(IServicesFactory factory, ITransaction transaction)
    {
        _allocationMap = factory.GetAllocationMap();
        _indexPage = factory.GetIndexPageService();
        _writer = factory.GetBsonWriter();

        _transaction = transaction;
        _collation = factory.FileHeader!.Value.Collation;
        _random = new();
    }

    /// <summary>
    /// Create head and tail nodes for a new index
    /// </summary>
    public async Task<(IndexNode head, IndexNode tail)> CreateHeadTailNodesAsync(byte colID)
    {
        // get how many bytes needed for each head/tail (both has same size)
        var bytesLength = (ushort)IndexNode.GetNodeLength(MAX_LEVEL_LENGTH, BsonValue.MinValue, out _);

        // get a new empty index page for this collection
        var page = await _transaction.GetFreePageAsync(colID, PageType.Index, PAGE_CONTENT_SIZE);

        // add head/tail nodes into page
        var head = _indexPage.InsertIndexNode(page, 0, MAX_LEVEL_LENGTH, BsonValue.MinValue, PageAddress.Empty, bytesLength);
        var tail = _indexPage.InsertIndexNode(page, 0, MAX_LEVEL_LENGTH, BsonValue.MaxValue, PageAddress.Empty, bytesLength);

        // link head-to-tail with double link list in first level
        head.SetNext(page, 0, tail.RowID);
        tail.SetPrev(page, 0, head.RowID);

        return (head, tail);
    }

    /// <summary>
    /// Insert a new node index inside an collection index. Flip coin to know level
    /// </summary>
    public async Task<IndexNode> AddNodeAsync(byte colID, IndexDocument index, BsonValue key, PageAddress dataBlock, IndexNode? last)
    {
        // do not accept Min/Max value as index key (only head/tail can have this value)
        if (key.IsMaxValue || key.IsMinValue) throw ERR($"BsonValue MaxValue/MinValue are not supported as index key");

        // random level (flip coin mode) - return number between 0-31
        var level = this.Flip();

        // call AddNode with key value
        return await this.AddNodeAsync(colID, index, key, dataBlock, level, last);
    }

    /// <summary>
    /// Insert a new node index inside an collection index.
    /// </summary>
    private async Task<IndexNode> AddNodeAsync(byte colID, IndexDocument index, BsonValue key, PageAddress dataBlock, byte level, IndexNode? last)
    {
        // get a free index page for head note
        var bytesLength = (ushort)IndexNode.GetNodeLength(level, key, out var keyLength);

        // test for index key maxlength
        if (keyLength > MAX_INDEX_KEY_LENGTH) throw ERR($"Index key must be less than {MAX_INDEX_KEY_LENGTH} bytes.");

        // get page with avaiable space to add this node
        var page = await _transaction.GetFreePageAsync(colID, PageType.Index, bytesLength);

        // create node in buffer
        var createdNode = _indexPage.InsertIndexNode(page,index.Slot, level, key, dataBlock, bytesLength);

        // now, let's link my index node on right place
        var current = await this.GetNodeAsync(index.Head, true);

        // using as cache last
        IndexNode? cache = null;

        // scan from top left
        for (int i = MAX_LEVEL_LENGTH - 1; i >= 0; i--)
        {
            // head/tail are not linked between 1-31 (level 0 always has a next)
            if (current.node.Next[i].IsEmpty) continue;

            // get current from next
            current = await this.GetNodeAsync(current.node.Next[i], true);

            // get cache for last node
            //**cache = cache != null && cache.Value.Position == current.node.Next[i] ? cache : await this.GetNodeAsync(current.node.Next[i], true);

            // for(; <while_not_this>; <do_this>) { ... }
            //for (; current.node.Next[i].IsEmpty == false; current = cache.Value)
            //{
            //    // get cache for last node
            //    cache = cache != null && cache.Value.Position == current.Next[i] ? cache : await this.GetNodeAsync(current.Next[i], true);

            //    // read next node to compare
            //    var diff = cache.Value.Key.CompareTo(key, _collation);

            //    // if unique and diff = 0, throw index exception (must rollback transaction - others nodes can be dirty)
            //    if (diff == 0 && index.Unique) throw LiteException.IndexDuplicateKey(index.Name, key);

            //    if (diff == 1) break;
            //}

            //if (i <= (level - 1)) // level == length
            //{
            //    // cur = current (immediately before - prev)
            //    // node = new inserted node
            //    // next = next node (where cur is pointing)

            //    createdNode.SetNext((byte)i, current.Next[i], );
            //    createdNode.SetPrev((byte)i, current.Position);
            //    current.SetNext((byte)i, createdNode.Position);

            //    var next = this.GetNode(createdNode.Next[i]);

            //    if (next != null)
            //    {
            //        next.SetPrev((byte)i, createdNode.Position);
            //    }
            //}
        }

        //// if last node exists, create a double link list
        //if (last != null)
        //{
        //    ENSURE(last.Value.NextNode == PageAddress.Empty, "last index node must point to null");

        //    // reload 'last' index node in case the IndexPage has gone through a defrag
        //    last = this.GetNode(last.Position);
        //    last.SetNextNode(createdNode.Position);
        //}

        return createdNode;
    }


    /// <summary>
    /// Flip coin - skip list - returns level node (start in 0)
    /// </summary>
    public byte Flip()
    {
        byte level = 0;

        for (int R = _random.Next(); (R & 1) == 1; R >>= 1)
        {
            level++;
            if (level == MAX_LEVEL_LENGTH - 1) break;
        }

        return level;
    }

    /// <summary>
    /// Get a node/pageBuffer inside a page using PageAddress. RowID must be a valid position
    /// </summary>
    public async Task<(IndexNode node, PageBuffer page)> GetNodeAsync(PageAddress rowID, bool writable)
    {
        /* BUSCA DA CACHE ?? PODE SER ALTERAVEL! */
        var page = await _transaction.GetPageAsync(rowID.PageID, writable);

        return (new IndexNode(page, rowID), page);
    }

    /*
        /// <summary>
        /// Gets all node list from passed node rowid (forward only)
        /// </summary>
        public async IAsyncEnumerable<IndexNode> GetNodeListAsync(PageAddress rowID, bool writable)
        {
            while (rowID.IsEmpty)
            {
                var node = await this.GetNodeAsync(rowID, writable);

                yield return node;

                rowID = node.NextNode;
            }
        }

        /// <summary>
        /// Deletes all indexes nodes from pkNode
        /// </summary>
        public void DeleteAll(PageAddress pkAddress)
        {
            var node = this.GetNode(pkAddress);
            var indexes = _snapshot.CollectionPage.GetCollectionIndexesSlots();

            while (node != null)
            {
                this.DeleteSingleNode(node, indexes[node.Slot]);

                // move to next node
                node = this.GetNode(node.NextNode);
            }
        }

        /// <summary>
        /// Deletes all list of nodes in toDelete - fix single linked-list and return last non-delete node
        /// </summary>
        public IndexNode DeleteList(PageAddress pkAddress, HashSet<PageAddress> toDelete)
        {
            var last = this.GetNode(pkAddress);
            var node = this.GetNode(last.NextNode); // starts in first node after PK
            var indexes = _snapshot.CollectionPage.GetCollectionIndexesSlots();

            while (node != null)
            {
                if (toDelete.Contains(node.Position))
                {
                    this.DeleteSingleNode(node, indexes[node.Slot]);

                    // fix single-linked list from last non-delete delete
                    last.SetNextNode(node.NextNode);
                }
                else
                {
                    // last non-delete node to set "NextNode"
                    last = node;
                }

                // move to next node
                node = this.GetNode(node.NextNode);
            }

            return last;
        }

        /// <summary>
        /// Delete a single index node - fix tree double-linked list levels
        /// </summary>
        private void DeleteSingleNode(IndexNode node, CollectionIndex index)
        {
            for (int i = node.Level - 1; i >= 0; i--)
            {
                // get previous and next nodes (between my deleted node)
                var prevNode = this.GetNode(node.Prev[i]);
                var nextNode = this.GetNode(node.Next[i]);

                if (prevNode != null)
                {
                    prevNode.SetNext((byte)i, node.Next[i]);
                }
                if (nextNode != null)
                {
                    nextNode.SetPrev((byte)i, node.Prev[i]);
                }
            }

            node.Page.DeleteIndexNode(node.Position.Index);

            _snapshot.AddOrRemoveFreeIndexList(node.Page, ref index.FreeIndexPageList);
        }

        /// <summary>
        /// Delete all index nodes from a specific collection index. Scan over all PK nodes, read all nodes list and remove
        /// </summary>
        public void DropIndex(CollectionIndex index)
        {
            var slot = index.Slot;
            var pkIndex = _snapshot.CollectionPage.PK;

            foreach(var pkNode in this.FindAll(pkIndex, Query.Ascending))
            {
                var next = pkNode.NextNode;
                var last = pkNode;

                while (next != PageAddress.Empty)
                {
                    var node = this.GetNode(next);

                    if (node.Slot == slot)
                    {
                        // delete node from page (mark as dirty)
                        node.Page.DeleteIndexNode(node.Position.Index);

                        last.SetNextNode(node.NextNode);
                    }
                    else
                    {
                        last = node;
                    }

                    next = node.NextNode;
                }
            }

            // removing head/tail index nodes
            this.GetNode(index.Head).Page.DeleteIndexNode(index.Head.Index);
            this.GetNode(index.Tail).Page.DeleteIndexNode(index.Tail.Index);
        }
    */
    #region Find

    /// <summary>
    /// Return all index nodes from an index
    /// </summary>
    public async IAsyncEnumerable<IndexNode> FindAll(IndexDocument index, int order)
    {
        var cur = order == Query.Ascending ? 
            await this.GetNodeAsync(index.Tail, false) : await this.GetNodeAsync(index.Head, false);

        var next = cur.node.GetNextPrev(0, order);

        while (!next.IsEmpty)
        {
            cur = await this.GetNodeAsync(next, false);

            // stop if node is head/tail
            if (cur.node.Key.IsMinValue || cur.node.Key.IsMaxValue) yield break;

            yield return cur.node;
        }
    }

    /// <summary>
    /// Find first node that index match with value . 
    /// If index are unique, return unique value - if index are not unique, return first found (can start, middle or end)
    /// If not found but sibling = true, returns near node (only non-unique index)
    /// </summary>
    public async Task<IndexNode?> Find(IndexDocument index, BsonValue value, bool sibling, int order)
    {
        var cur = order == Query.Ascending ? 
            await this.GetNodeAsync(index.Head, false) : 
            await this.GetNodeAsync(index.Tail, false);

        //for (int i = index.MaxLevel - 1; i >= 0; i--)
        //{
        //    for (; cur.GetNextPrev((byte)i, order).IsEmpty == false; cur = this.GetNode(cur.GetNextPrev((byte)i, order)))
        //    {
        //        var next = this.GetNode(cur.GetNextPrev((byte)i, order));
        //        var diff = next.Key.CompareTo(value, _collation);

        //        if (diff == order && (i > 0 || !sibling)) break;
        //        if (diff == order && i == 0 && sibling)
        //        {
        //            // is head/tail?
        //            return (next.Key.IsMinValue || next.Key.IsMaxValue) ? null : next;
        //        }

        //        // if equals, return index node
        //        if (diff == 0)
        //        {
        //            return next;
        //        }
        //    }
        //}

        return null;
    }

    #endregion
}