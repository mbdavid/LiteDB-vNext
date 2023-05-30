namespace LiteDB.Engine;

/// <summary>
/// Represent a index node inside a Index Page
/// </summary>
internal struct IndexNode
{
    /// <summary>
    /// Fixed length of IndexNode (12 bytes)
    /// </summary>
    private const int INDEX_NODE_FIXED_SIZE = 1 + // Slot [1 byte]
                                              1 + // Levels [1 byte]
                                              PageAddress.SIZE + // DataBlock (5 bytes)
                                              PageAddress.SIZE;  // NextNode (5 bytes)

    private const int P_SLOT = 0; // 00-00 [byte]
    private const int P_LEVEL = 1; // 01-01 [byte]
    private const int P_DATA_BLOCK = 2; // 02-06 [PageAddress]
    private const int P_NEXT_NODE = 7; // 07-11 [PageAddress]
    private const int P_PREV_NEXT = 12; // 12-(_level * 5 [PageAddress] * 2 [prev-next])
    private static int P_KEY(byte level) => P_PREV_NEXT + (level * PageAddress.SIZE * 2); // just after NEXT

    /// <summary>
    /// Index position of this node inside a IndexPage (not persist)
    /// </summary>
    public readonly PageAddress RowID;

    /// <summary>
    /// Index slot reference in CollectionIndex [1 byte]
    /// </summary>
    public readonly byte Slot;

    /// <summary>
    /// Skip-list level (0-31) - [1 byte]
    /// </summary>
    public readonly byte Level;

    /// <summary>
    /// The object value that was indexed (max 255 bytes value)
    /// </summary>
    public readonly BsonValue Key;

    /// <summary>
    /// Reference for a datablock address
    /// </summary>
    public readonly PageAddress DataBlock;

    /// <summary>
    /// Single linked-list for all nodes from a single document [5 bytes]
    /// </summary>
    public PageAddress NextNode;

    /// <summary>
    /// Link to prev value (used in skip lists - Prev.Length = Next.Length) [5 bytes]
    /// </summary>
    public readonly PageAddress[] Prev;

    /// <summary>
    /// Link to next value (used in skip lists - Prev.Length = Next.Length)
    /// </summary>
    public readonly PageAddress[] Next;

    /// <summary>
    /// Read index node from page block
    /// </summary>
    public IndexNode(PageBuffer page, PageAddress rowID)
    {
        this.RowID = rowID; // reference position (PageID+Index)

        var segment = PageSegment.GetSegment(page, rowID.Index);
        var span = page.AsSpan(segment);

        this.Slot = span[P_SLOT];
        this.Level = span[P_LEVEL];
        this.DataBlock = span[P_DATA_BLOCK..].ReadPageAddress();
        this.NextNode = span[P_NEXT_NODE..].ReadPageAddress();

        this.Next = new PageAddress[this.Level];
        this.Prev = new PageAddress[this.Level];

        for (var i = 0; i < this.Level; i++)
        {
            var prevAddr = P_PREV_NEXT + (i * PageAddress.SIZE * 2);
            var nextAddr = P_PREV_NEXT + (i * PageAddress.SIZE * 2) + PageAddress.SIZE;

            this.Prev[i] = span[prevAddr..].ReadPageAddress();
            this.Next[i] = span[nextAddr..].ReadPageAddress();
        }

        var keyPosition = P_KEY(this.Level);

        // read bson value from buffer
        this.Key = span[keyPosition..].ReadBsonValue(out _);
    }

    /// <summary>
    /// Create new index node and persist into page block
    /// </summary>
    public IndexNode(PageBuffer page, PageAddress rowID, byte slot, byte level, BsonValue key, PageAddress dataBlock)
    {
        this.RowID = rowID;

        this.Slot = slot;
        this.Level = level;
        this.DataBlock = dataBlock;
        this.NextNode = PageAddress.Empty;
        this.Next = new PageAddress[level];
        this.Prev = new PageAddress[level];
        this.Key = key;

        var segment = PageSegment.GetSegment(page, rowID.Index);
        var span = page.AsSpan(segment);

        // persist in buffer read only data
        span[P_SLOT] = slot;
        span[P_LEVEL] = level;
        span[P_DATA_BLOCK..].WritePageAddress(dataBlock);
        span[P_NEXT_NODE..].WritePageAddress(this.NextNode);

        for (byte i = 0; i < level; i++)
        {
            this.SetPrev(i, PageAddress.Empty, span);
            this.SetNext(i, PageAddress.Empty, span);
        }

        var keyPosition = P_KEY(this.Level);

        // writing key value
        span[keyPosition..].WriteBsonValue(this.Key, out _);
    }

    /// <summary>
    /// Create a fake index node used only in Virtual Index runner
    /// </summary>
    public IndexNode(BsonDocument doc)
    {
        this.RowID = new PageAddress(0, 0);
        this.Slot = 0;
        this.Level = 0;
        this.DataBlock = PageAddress.Empty;
        this.NextNode = PageAddress.Empty;
        this.Next = new PageAddress[0];
        this.Prev = new PageAddress[0];

        // index node key IS document
        this.Key = doc;
    }

    /// <summary>
    /// Update NextNode pointer (update in buffer too). Also, set page as dirty
    /// </summary>
    public void SetNextNode(PageAddress value, Span<byte> span)
    {
        this.NextNode = value;

        span[P_NEXT_NODE..].WritePageAddress(value);
    }

    /// <summary>
    /// Update Prev[index] pointer (update in buffer too).
    /// </summary>
    public void SetPrev(byte level, PageAddress value, PageBuffer page)
        => SetPrev(level, value, page.AsSpan(this.Location));

    /// <summary>
    /// Update Prev[index] pointer (update in buffer too).
    /// </summary>
    public void SetPrev(PageBuffer page, byte level, PageAddress value)
    {
        ENSURE(level <= this.Level, "out of index in level");

        this.Prev[level] = value;

        var prevAddr = P_PREV_NEXT + (level * PageAddress.SIZE * 2);

        span[prevAddr..].WritePageAddress(value);
    }

    /// <summary>
    /// Update Next[index] pointer (update in buffer too).
    /// </summary>
    public void SetNext(byte level, PageAddress value, PageBuffer page)
        => SetNext(level, value, page.AsSpan(this.Location));

    /// <summary>
    /// Update Next[index] pointer (update in buffer too).
    /// </summary>
    public void SetNext(byte level, PageAddress value, Span<byte> span)
    {
        ENSURE(level <= this.Level, "out of index in level");

        this.Next[level] = value;

        var nextAddr = P_NEXT_NODE + (level * PageAddress.SIZE * 2) + PageAddress.SIZE;

        span[nextAddr..].WritePageAddress(value);
    }

    /// <summary>
    /// Returns Next (order == 1) OR Prev (order == -1)
    /// </summary>
    public PageAddress GetNextPrev(byte level, int order)
    {
        return order == Query.Ascending ? this.Next[level] : this.Prev[level];
    }

    #region Static Helpers

    /// <summary>
    /// Calculate how many bytes this node will need on page block
    /// </summary>
    public static int GetNodeLength(byte level, BsonValue key, out int keyLength)
    {
        keyLength = GetKeyLength(key);

        return INDEX_NODE_FIXED_SIZE +
            (level * 2 * PageAddress.SIZE) + // prev/next
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

        var varLength = (key.IsString || key.IsBinary) ? BsonValue.GetVariantLength(keyLength) : 0;

        return 1 +      // BsonType
            varLength + // Variable Length (0, 1, 2, 4)
            keyLength;  // Key Length
    }

    #endregion

    public override string ToString()
    {
        return $"RowID: [{this.RowID}] - Key: {this.Key}";
    }
}
