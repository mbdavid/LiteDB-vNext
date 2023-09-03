﻿namespace LiteDB.Engine;

/// <summary>
/// Implement a Index service - Add/Remove index nodes on SkipList
/// Based on: http://igoro.com/archive/skip-lists-are-fascinating/
/// </summary>
[AutoInterface]
unsafe internal class IndexService : IIndexService
{
    // dependency injection
    private readonly IIndexPageModifier _indexPageModifier;
    private readonly ITransaction _transaction;
    private readonly Collation _collation;

    public IndexService(
        IIndexPageModifier indexPageModifier,
        Collation collation,
        ITransaction transaction)
    {
        _indexPageModifier = indexPageModifier;
        _collation = collation;
        _transaction = transaction;
    }

    /// <summary>
    /// Create head and tail nodes for a new index
    /// </summary>
    public (RowID head, RowID tail) CreateHeadTailNodes(byte colID)
    {
        // get how many bytes needed for each head/tail (both has same size)
        var bytesLength = (ushort)IndexNode.GetNodeLength(INDEX_MAX_LEVELS, IndexKey.MinValue);

        // get a index page for this collection
        var pagePtr = _transaction.GetFreeIndexPage(colID, bytesLength);

        // get initial pageExtend value
        var before = pagePtr->ExtendPageValue;

        // add head/tail nodes into page
        var head = _indexPageModifier.InsertIndexNode(pagePtr, 0, INDEX_MAX_LEVELS, IndexKey.MinValue, RowID.Empty, bytesLength);
        var tail = _indexPageModifier.InsertIndexNode(pagePtr, 0, INDEX_MAX_LEVELS, IndexKey.MaxValue, RowID.Empty, bytesLength);

        // link head-to-tail with double link list in first level
        head.Levels->NextID = tail.IndexNodeID;
        tail.Levels->PrevID = head.IndexNodeID;

        // update allocation map if needed
        var after = pagePtr->ExtendPageValue;

        if (before != after)
        {
            _transaction.UpdatePageMap(pagePtr->PageID, after);
        }

        return (head.IndexNodeID, tail.IndexNodeID);
    }

    /// <summary>
    /// Insert a new node index inside an collection index. Flip coin to know level
    /// </summary>
    public IndexNodeResult AddNode(byte colID, IndexDocument index, IndexKey indexKey, RowID dataBlockID, IndexNodeResult head, IndexNodeResult last)
    {
        using var _pc = PERF_COUNTER(4, nameof(AddNode), nameof(IndexService));

        // random level (flip coin mode) - return number between 0-31
        var levels = this.Flip();

        // call AddNode with key value
        return this.AddNodeInternal(colID, index, indexKey, dataBlockID, levels, head, last);
    }

    /// <summary>
    /// Insert a new node index inside an collection index.
    /// </summary>
    private IndexNodeResult AddNodeInternal(byte colID, IndexDocument index, IndexKey indexKey, RowID dataBlockID, int insertLevels, IndexNodeResult head, IndexNodeResult last)
    {
        // get a free index page for head note
        var bytesLength = (ushort)IndexNode.GetNodeLength(insertLevels, indexKey);

        // get an index page with avaliable space to add this node
        var pagePtr = _transaction.GetFreeIndexPage(colID, bytesLength);

        // get initial pageValue
        var before = pagePtr->ExtendPageValue;

        // create node in page
        var node = _indexPageModifier.InsertIndexNode(pagePtr, index.Slot, (byte)insertLevels, indexKey, dataBlockID, bytesLength);

        // update allocation map if needed (this page has no more "size" changes)
        var after = pagePtr->ExtendPageValue;

        if (before != after)
        {
            _transaction.UpdatePageMap(pagePtr->PageID, after);
        }

        // now, let's link my index node on right place
        var leftNode = head;

        // for: scan from top to bottom
        for (int currentLevel = INDEX_MAX_LEVELS - 1; currentLevel >= 0; currentLevel--)
        {
            var right = leftNode[currentLevel]->NextID;

            // while: scan from left to right
            while (right.IsEmpty == false && right != index.TailIndexNodeID)
            {
                var rightNode = this.GetNode(right);

                // read next node to compare
                //***var diff = rightNode.Node.Key.CompareTo(key, _collation);
                var diff = IndexKey.Compare(rightNode.Key, &indexKey, _collation);

                //***if unique and diff == 0, throw index exception (must rollback transaction - others nodes can be dirty)
                if (diff == 0 && index.Unique) throw ERR("IndexDuplicateKey(index.Name, key)");

                if (diff == 1) break; // stop going right

                leftNode = rightNode;
                //***right = rightNode.Node.Next[currentLevel];
                right = rightNode[currentLevel]->NextID;
            }

            if (currentLevel <= insertLevels - 1) // level == length
            {
                // prev: immediately before new node
                // node: new inserted node
                // next: right node from prev (where left is pointing)

                //***var prev = leftNode.Node.IndexNodeID;
                //***var next = leftNode.Node.Next[currentLevel];
                var prev = leftNode.IndexNodeID;
                var next = leftNode[currentLevel]->NextID;

                // if next is empty, use tail (last key)
                if (next.IsEmpty) next = index.TailIndexNodeID;

                // set new node pointer links with current level sibling
                //***node.SetNext(page, currentLevel, next);
                //***node.SetPrev(page, currentLevel, prev);
                node[currentLevel]->NextID = next;
                node[currentLevel]->PrevID = prev;

                // fix sibling pointer to new node
                //***leftNode.Node.SetNext(leftNode.Page, currentLevel, node.IndexNodeID);
                leftNode[currentLevel]->NextID = node.IndexNodeID;

                //***right = node.Next[currentLevel];
                right = node[currentLevel]->NextID;

                //***var rightNode = await this.GetNodeAsync(right);
                //***rightNode.Node.SetPrev(rightNode.Page, currentLevel, node.IndexNodeID);
                var rightNode = this.GetNode(right);
                rightNode[currentLevel]->PrevID = node.IndexNodeID;

            }

        }

        // if last node exists, create a single link list between node list
        if (!last.IsEmpty)
        {
            // set last node to link with current node
            //***last.Node.SetNextNodeID(last.Page, node.IndexNodeID);
            last.Node->NextNodeID = node.IndexNodeID;
        }

        return node;
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
    /// Get a node/pageBuffer inside a page using RowID. IndexNodeID must be a valid position
    /// </summary>
    public IndexNodeResult GetNode(RowID indexNodeID)
    {
        using var _pc = PERF_COUNTER(5, nameof(GetNode), nameof(IndexService));

        var pagePtr = _transaction.GetPage(indexNodeID.PageID);

        ENSURE(pagePtr->PageType == PageType.Index, new { indexNodeID });

        var result = _indexPageModifier.GetIndexNode(pagePtr, indexNodeID.Index);

        return result;
    }

    #region Find

    /// <summary>
    /// Find first node that index match with value . 
    /// If index are unique, return unique value - if index are not unique, return first found (can start, middle or end)
    /// If not found but sibling = true and key are not found, returns next value index node (if order = Asc) or prev node (if order = Desc)
    /// </summary>
    public IndexNodeResult Find(IndexDocument index, IndexKey key, bool sibling, int order)
    {
        var left = order == Query.Ascending ? index.HeadIndexNodeID : index.TailIndexNodeID;
        var leftNode = this.GetNode(left);

        for (var level = INDEX_MAX_LEVELS - 1; level >= 0; level--)
        {
            //***var right = leftNode.Node.GetNextPrev(level, order);
            var right = leftNode[level]->GetNextPrev(order);

            while (right.IsEmpty == false)
            {
                var rightNode = this.GetNode(right);

                //var diff = rightNode.Node.Key.CompareTo(key, _collation);
                var diff = IndexKey.Compare(rightNode.Key, &key, _collation);

                if (diff == order && (level > 0 || !sibling)) break; // go down one level

                if (diff == order && level == 0 && sibling)
                {
                    // is head/tail?
                    //***return (rightNode.Node.Key.IsMinValue || rightNode.Node.Key.IsMaxValue) ? __IndexNodeResult.Empty : rightNode;
                    return (rightNode.Key->IsMinValue || rightNode.Key->IsMaxValue) ? IndexNodeResult.Empty : rightNode;
                }

                // if equals, return index node
                if (diff == 0)
                {
                    return rightNode;
                }

                leftNode = rightNode;
                //***right = rightNode.Node.GetNextPrev(level, order);
                right = rightNode[level]->GetNextPrev(order);
            }
        }

        return IndexNodeResult.Empty;
    }

    #endregion

    /// <summary>
    /// Deletes all indexes nodes from pkNode
    /// </summary>
    public void DeleteAllAsync(IndexNodeResult node)
    {
        // all indexes nodes from a document are connected by nextNode
        while (!node.IsEmpty)
        {
            this.DeleteSingleNode(node);

            if (node.Node->NextNodeID.IsEmpty) break;

            // move to next node
            node = this.GetNode(node.Node->NextNodeID);
        }
    }

    /// <summary>
    /// Delete a single node fixing all next/prev levels pointers
    /// </summary>
    private void DeleteSingleNode(IndexNodeResult node)
    {
        // run over all levels linking prev with next
        for (int i = node.Node->Levels - 1; i >= 0; i--)
        {
            // get previous and next nodes (between my deleted node)
            //***var prevNode, prevPage) = this.GetNode(nodePtr.Prev[i]);
            //***var nextNode, nextPage) = this.GetNode(nodePtr.Next[i]);
            var prev = this.GetNode(node[i]->PrevID);
            var next = this.GetNode(node[i]->NextID);

            //***if (!prevNode.IsEmpty)
            //***{
            //***    prevNode.SetNext(prevPage, (byte)i, nodePtr.Next[i]);
            //***}
            //***
            //***if (!nextNode.IsEmpty)
            //***{
            //***    nextNode.SetPrev(nextPage, (byte)i, nodePtr.Prev[i]);
            //***}
            if (!prev.IsEmpty)
            {
                prev[i]->NextID = node[i]->NextID;
            }

            if (!next.IsEmpty)
            {
                next[i]->PrevID = node[i]->PrevID;
            }
        }

        // get extend page value before page change
        var before = node.Page->ExtendPageValue;

        // delete node segment in page
        _indexPageModifier.DeleteIndexNode(node.Page, node.IndexNodeID.Index);

        // update map page only if change page value
        var after = node.Page->ExtendPageValue;

        if (before != after)
        {
            _transaction.UpdatePageMap(node.Page->PageID, after);
        }
    }
}