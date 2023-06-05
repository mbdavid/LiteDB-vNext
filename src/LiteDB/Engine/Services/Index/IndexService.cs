using System.Data;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;
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
        _collation = factory.GetDisk().FileHeader.Collation;
        _random = new();
    }

    /// <summary>
    /// Create head and tail nodes for a new index
    /// </summary>
    public async Task<(IndexNode head, IndexNode tail)> CreateHeadTailNodesAsync(byte colID)
    {
        // get how many bytes needed for each head/tail (both has same size)
        var bytesLength = (ushort)IndexNode.GetNodeLength(INDEX_MAX_LEVELS, BsonValue.MinValue, out _);

        // get a new empty index page for this collection
        var page = await _transaction.GetFreePageAsync(colID, PageType.Index, PAGE_CONTENT_SIZE);

        // add head/tail nodes into page
        var head = _indexPage.InsertIndexNode(page, 0, INDEX_MAX_LEVELS, BsonValue.MinValue, PageAddress.Empty, bytesLength);
        var tail = _indexPage.InsertIndexNode(page, 0, INDEX_MAX_LEVELS, BsonValue.MaxValue, PageAddress.Empty, bytesLength);

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
        var levels = this.Flip();

        // call AddNode with key value
        return await this.AddNodeAsync(colID, index, key, dataBlock, levels, last);
    }

    /// <summary>
    /// Insert a new node index inside an collection index.
    /// </summary>
    private async Task<IndexNode> AddNodeAsync(byte colID, IndexDocument index, BsonValue key, PageAddress dataBlock, int insertLevels, IndexNode? last)
    {
        // get a free index page for head note
        var bytesLength = (ushort)IndexNode.GetNodeLength(insertLevels, key, out var keyLength);

        // test for index key maxlength
        if (keyLength > INDEX_MAX_KEY_LENGTH) throw ERR($"Index key must be less than {INDEX_MAX_KEY_LENGTH} bytes.");

        // get page with avaiable space to add this node
        var page = await _transaction.GetFreePageAsync(colID, PageType.Index, bytesLength);

        // create node in buffer
        var node = _indexPage.InsertIndexNode(page,index.Slot, insertLevels, key, dataBlock, bytesLength);

        // now, let's link my index node on right place
        var left = index.Head;
        var leftNode = await this.GetNodeAsync(left, true);

        // for: scan from top to bottom
        for (int i = INDEX_MAX_LEVELS - 1; i >= 0; i--)
        {
            var currentLevel = (byte)i;
            var right = leftNode.node.Next[currentLevel];

            // while: scan from left to right
            while (right.IsEmpty == false)
            {
                var rightNode = await this.GetNodeAsync(right, true);

                // read next node to compare
                var diff = rightNode.node.Key.CompareTo(key, _collation);

                // if unique and diff == 0, throw index exception (must rollback transaction - others nodes can be dirty)
                if (diff == 0 && index.Unique) throw ERR("IndexDuplicateKey(index.Name, key)");

                if (diff == 1) break; // stop going right

                leftNode = rightNode;
                right = rightNode.node.Next[currentLevel];
            }

            if (currentLevel <= insertLevels - 1) // level == length
            {
                // prev: immediately before new node
                // node: new inserted node
                // next: right node from prev (where left is pointing)

                var prev = leftNode.node.RowID;
                var next = leftNode.node.Next[currentLevel];

                // if next is empty, use tail (last key)
                if (next.IsEmpty) next = index.Tail;

                // set new node pointer links with current level sibling
                node.SetNext(page, currentLevel, next);
                node.SetPrev(page, currentLevel, prev);
                
                // fix sibling pointer to new node
                leftNode.node.SetNext(leftNode.page, currentLevel, node.RowID);

                right = node.Next[currentLevel];

                var rightNode = await this.GetNodeAsync(right, true);
                rightNode.node.SetPrev(rightNode.page, currentLevel, node.RowID);
            }
        }

        //// if last node exists, create a double link list
        //if (last != null)
        //{
        //    ENSURE(last.Value.NextNode == PageAddress.Empty, "last index node must point to null");

        //    // reload 'last' index node in case the IndexPage has gone through a defrag
        //    last = this.GetNode(last.Position);
        //    last.SetNextNode(createdNode.Position);
        //}

        return node;
    }


    public static byte[] altura = new byte[] { 2, 5, 1, 4, 3, 2, 1, 2, 1, 4 };
    public static int prox = 0;

    /// <summary>
    /// Flip coin (skipped list): returns how many levels the node will have (starts in 1, max of INDEX_MAX_LEVELS)
    /// </summary>
    public int Flip()
    {
        return altura[prox++];

        byte levels = 1;

        for (int R = _random.Next(); (R & 1) == 1; R >>= 1)
        {
            levels++;
            if (levels == INDEX_MAX_LEVELS) break;
        }

        return levels;
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

    #region Find

    /// <summary>
    /// Return all index nodes from an index
    /// </summary>
    public async IAsyncEnumerable<IndexNode> FindAllAsync(IndexDocument index, int order)
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
    public async Task<IndexNode?> FindAsync(IndexDocument index, BsonValue key, bool sibling, int order)
    {
        var left = order == Query.Ascending ? index.Head : index.Tail;
        var leftNode = await this.GetNodeAsync(left, false);

        for (var level = INDEX_MAX_LEVELS - 1; level >= 0; level--)
        {
            var right = leftNode.node.GetNextPrev(level, order);

            while (right.IsEmpty == false)
            {
                var rightNode = await this.GetNodeAsync(right, false);

                var diff = rightNode.node.Key.CompareTo(key, _collation);
                
                if (diff == order && (level > 0 || !sibling)) break; // go down one level

                if (diff == order && level == 0 && sibling)
                {
                    // is head/tail?
                    return (rightNode.node.Key.IsMinValue || rightNode.node.Key.IsMaxValue) ? null : rightNode.node;
                }

                // if equals, return index node
                if (diff == 0)
                {
                    return rightNode.node;
                }

                leftNode = rightNode;
                right = rightNode.node.GetNextPrev(level, order);
            }

        }

        return null;
    }

    #endregion
}