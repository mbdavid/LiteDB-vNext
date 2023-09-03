namespace LiteDB.Engine;

/// <summary>
/// Represent a index node inside a Index Page
/// </summary>
[Obsolete]
internal struct __IndexNode
{
    public static readonly __IndexNode Empty = new();

    /// <summary>
    /// Fixed length of __IndexNode (13 bytes)
    /// </summary>
    private const int INDEX_NODE_FIXED_SIZE = 1 + // Slot (1 byte)
                                              1 + // Index type (1 byte)
                                              1 + // Levels (1 byte)
                                              PageAddress.SIZE + // __DataBlock (5 bytes)
                                              PageAddress.SIZE;  // NextNode (5 bytes)

    private const int P_SLOT = 0; // 00-00 [byte]
    private const int P_INDEX_TYPE = 1; // 01-01 [byte] (reserved)
    private const int P_LEVEL = 2; // 02-02 [byte]
    private const int P_DATA_BLOCK = 3; // 03-07 [PageAddress]
    private const int P_NEXT_NODE = 8; // 08-12 [PageAddress]
    private const int P_PREV_NEXT = 13; // 13-(_level * 5 [PageAddress] * 2 [prev-next])
    private static int P_KEY(int levels) => P_PREV_NEXT + (levels * PageAddress.SIZE * 2); // just after NEXT

    /// <summary>
    /// Index address of this node inside a IndexPage (not persist)
    /// </summary>
    public readonly PageAddress IndexNodeID;

    /// <summary>
    /// Index slot reference in CollectionIndex [1 byte]
    /// </summary>
    public readonly byte Slot;

    /// <summary>
    /// Index node type (1 = Skip list node) [1 byte] (reserved)
    /// </summary>
    public readonly byte IndexType;

    /// <summary>
    /// Amount of skipped-list levels (1-32) - [1 byte]
    /// </summary>
    public readonly byte Levels;

    /// <summary>
    /// The object value that was indexed (max 255 bytes value)
    /// </summary>
    public readonly BsonValue Key;

    /// <summary>
    /// Reference for a datablock address
    /// </summary>
    public readonly PageAddress DataBlockID;

    /// <summary>
    /// Single linked-list for all nodes from a single document [5 bytes]
    /// </summary>
    public PageAddress NextNodeID;

    /// <summary>
    /// Link to prev value (used in skip lists - Prev.Length = Next.Length) [5 bytes]
    /// </summary>
    public readonly PageAddress[] Prev;

    /// <summary>
    /// Link to next value (used in skip lists - Prev.Length = Next.Length)
    /// </summary>
    public readonly PageAddress[] Next;

    public bool IsEmpty => this.IndexNodeID.IsEmpty;

    /// <summary>
    /// Read index node from page block
    /// </summary>
    public __IndexNode(PageBuffer page, PageAddress indexNodeID)
    {
        using var _pc = PERF_COUNTER(6, "Ctor", nameof(__IndexNode));

        this.IndexNodeID = indexNodeID; // reference position (PageID+Index)

        var segment = PageSegment.GetSegment(page, indexNodeID.Index, out var _);
        var span = page.AsSpan(segment);

        this.Slot = span[P_SLOT];
        this.IndexType = span[P_INDEX_TYPE];
        this.Levels = span[P_LEVEL];
        this.DataBlockID = span[P_DATA_BLOCK..].ReadPageAddress();
        this.NextNodeID = span[P_NEXT_NODE..].ReadPageAddress();

        this.Next = new PageAddress[this.Levels];
        this.Prev = new PageAddress[this.Levels];

        for (var i = 0; i < this.Levels; i++)
        {
            var prevAddr = P_PREV_NEXT + (i * PageAddress.SIZE * 2);
            var nextAddr = P_PREV_NEXT + (i * PageAddress.SIZE * 2) + PageAddress.SIZE;

            this.Prev[i] = span[prevAddr..].ReadPageAddress();
            this.Next[i] = span[nextAddr..].ReadPageAddress();
        }

        var keyPosition = P_KEY(this.Levels);

        // read bson value from buffer
        this.Key = span[keyPosition..].ReadBsonValue(out _);
    }

    /// <summary>
    /// Create new index node and persist into page block
    /// </summary>
    public __IndexNode(PageBuffer page, PageAddress indexNodeID, byte slot, int levels, BsonValue key, PageAddress dataBlock)
    {
        page.IsDirty = true;

        this.IndexNodeID = indexNodeID;

        this.Slot = slot;
        this.IndexType = 1; // skip list node (reserved)
        this.Levels = (byte)levels;
        this.DataBlockID = dataBlock;
        this.NextNodeID = PageAddress.Empty;
        this.Next = new PageAddress[levels];
        this.Prev = new PageAddress[levels];
        this.Key = key;

        var segment = PageSegment.GetSegment(page, indexNodeID.Index, out var _);
        var span = page.AsSpan(segment);

        // persist in buffer read only data
        span[P_SLOT] = slot;
        span[P_INDEX_TYPE] = this.IndexType;
        span[P_LEVEL] = (byte)levels;
        span[P_DATA_BLOCK..].WritePageAddress(dataBlock);
        span[P_NEXT_NODE..].WritePageAddress(PageAddress.Empty);

        for (byte i = 0; i < levels; i++)
        {
            var prevAddr = P_PREV_NEXT + (i * PageAddress.SIZE * 2);
            var nextAddr = P_PREV_NEXT + (i * PageAddress.SIZE * 2) + PageAddress.SIZE;

            this.Prev[i] = this.Next[i] = PageAddress.Empty;

            span[prevAddr..].WritePageAddress(PageAddress.Empty);
            span[nextAddr..].WritePageAddress(PageAddress.Empty);
        }

        var keyPosition = P_KEY(levels);

        // writing key value
        span[keyPosition..].WriteBsonValue(key, out _);
    }

    /// <summary>
    /// Create a fake index node used only in Virtual Index runner
    /// </summary>
    public __IndexNode(BsonDocument doc)
    {
        this.IndexNodeID = new PageAddress(0, 0);
        this.Slot = 0;
        this.Levels = 1;
        this.DataBlockID = PageAddress.Empty;
        this.NextNodeID = PageAddress.Empty;
        this.Next = new PageAddress[this.Levels];
        this.Prev = new PageAddress[this.Levels];

        // index node key IS document
        this.Key = doc;
    }

    /// <summary>
    /// Empty __IndexNode singleton instance
    /// </summary>
    public __IndexNode()
    {
        this.IndexNodeID = PageAddress.Empty;
        this.Key = BsonValue.Null;
        this.Prev = Array.Empty<PageAddress>();
        this.Next = Array.Empty<PageAddress>();
    }

    /// <summary>
    /// Update NextNodeID pointer (update in buffer too)
    /// </summary>
    public void SetNextNodeID(PageBuffer page, PageAddress nextNodeID)
    {
        ENSURE(this.IndexNodeID.PageID == page.Header.PageID, $"should be same index page", new { self = this, page, nextNodeID });

        if (this.NextNodeID == nextNodeID) return;

        page.IsDirty = true;

        this.NextNodeID = nextNodeID;

        var segment = PageSegment.GetSegment(page, this.IndexNodeID.Index, out var _);
        var span = page.AsSpan(segment);

        span[P_NEXT_NODE..].WritePageAddress(nextNodeID);
    }

    /// <summary>
    /// Update Prev[index] pointer (update in buffer too).
    /// </summary>
    public void SetPrev(PageBuffer page, int level, PageAddress prev)
    {
        ENSURE(this.IndexNodeID.PageID == page.Header.PageID, $"Should be same index page", new { self = this, page, prev });
        ENSURE(level < this.Levels, "Out of index in level", new { self = this, page, level, prev });

        if (this.Prev[level] == prev) return;

        page.IsDirty = true;

        this.Prev[level] = prev;

        var prevAddr = P_PREV_NEXT + (level * PageAddress.SIZE * 2);

        var segment = PageSegment.GetSegment(page, this.IndexNodeID.Index, out var _);
        var span = page.AsSpan(segment);

        span[prevAddr..].WritePageAddress(prev);
    }

    /// <summary>
    /// Update Next[index] pointer (update in buffer too).
    /// </summary>
    public void SetNext(PageBuffer page, int level, PageAddress next)
    {
        ENSURE(this.IndexNodeID.PageID == page.Header.PageID, $"Should be same index page", new { self = this, page, next });
        ENSURE(level < this.Levels, "Out of index in level", new { self = this, page, level, next });

        if (this.Next[level] == next) return;

        page.IsDirty = true;

        this.Next[level] = next;

        var nextAddr = P_PREV_NEXT + (level * PageAddress.SIZE * 2) + PageAddress.SIZE;

        var segment = PageSegment.GetSegment(page, this.IndexNodeID.Index, out var _);
        var span = page.AsSpan(segment);

        span[nextAddr..].WritePageAddress(next);
    }

    /// <summary>
    /// Returns Next (order == 1) OR Prev (order == -1)
    /// </summary>
    public PageAddress GetNextPrev(int level, int order)
    {
        return order == Query.Ascending ? this.Next[level] : this.Prev[level];
    }

    #region Static Helpers

    /// <summary>
    /// Calculate how many bytes this node will need on page block
    /// </summary>
    public static int GetNodeLength(int levels, BsonValue key, out int keyLength)
    {
        keyLength = GetKeyLength(key);

        return INDEX_NODE_FIXED_SIZE +
            (levels * 2 * PageAddress.SIZE) + // prev/next
            keyLength; // key
    }

    /// <summary>
    /// Get how many bytes will be used to store this value. Must consider:
    /// [1 byte] - BsonType
    /// [1,2,4 bytes] - KeyLength (used only in String|Byte[])
    /// [N bytes] - BsonValue in bytes (0-254)
    /// </summary>
    public static int GetKeyLength(BsonValue key)
    {
        var keyLength = key.GetBytesCountCached();

        var varLength = (key.IsString || key.IsBinary) ? BsonValue.GetVariantLengthFromData(keyLength) : 0;

        return 1 +      // BsonType
            varLength + // Variable Length (0, 1, 2, 4)
            keyLength;  // Key Length
    }

    #endregion

    public override string ToString() => Dump.Object(this);
}
