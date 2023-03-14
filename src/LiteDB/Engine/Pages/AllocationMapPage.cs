namespace LiteDB.Engine;

/// <summary>
/// Represent a single allocation map page with 1.632 extends and 13.056 pages pointer
/// </summary>
internal class AllocationMapPage : BasePage
{
    /// <summary>
    /// Get how many extends exists in this page
    /// </summary>
    public int ExtendsCount => AMP_EXTEND_COUNT - _emptyExtends.Count;

    private readonly Queue<int> _emptyExtends;

    /// <summary>
    /// Create a new AllocationMapPage
    /// </summary>
    public AllocationMapPage(uint pageID, PageBuffer writeBuffer)
        : base(pageID, PageType.AllocationMap, writeBuffer)
    {
        // fill all queue as empty extends
        _emptyExtends = new Queue<int>(Enumerable.Range(0, AMP_EXTEND_COUNT));
    }

    /// <summary>
    /// Load AllocationMap from buffer memory
    /// </summary>
    public AllocationMapPage(PageBuffer buffer, IMemoryCacheService memoryCache)
        : base(buffer, memoryCache)
    {
        // create an empty list of extends
        _emptyExtends = new Queue<int>(AMP_EXTEND_COUNT);
    }

    /// <summary>
    /// Read all allocation map page and populate collectionFreePages from AllocationMap service instance
    /// </summary>
    public void ReadAllocationMap(CollectionFreePages[] collectionFreePages)
    {
        // if this page contais all empty extends, there is no need to read all buffer
        if (_emptyExtends.Count == AMP_EXTEND_COUNT) return;

        var span = _readBuffer.AsSpan();

        ENSURE(_emptyExtends.Count == 0, "empty extends will be loaded here and can't have any page before here");

        for (var i = 0; i < AMP_EXTEND_COUNT; i++)
        {
            var position = PAGE_HEADER_SIZE + (i * AMP_EXTEND_SIZE);

            // check if empty colID (means empty extend)
            var colID = span[position];

            if (colID == 0)
            {
                DEBUG(span[position..(position + AMP_BYTES_PER_EXTEND)].IsFullZero(), $"all page extend allocation map should be empty at {position}");

                _emptyExtends.Enqueue(i);
            }
            else
            {
                var pagesBytes = span[(position + 1)..(position + AMP_BYTES_PER_EXTEND - 2)];

                //TODO: Cassiano, está errado. O ExtendID deve ser um int no banco inteiro...
                // precisa levar em consideração a PageID 
                var extendID = i;

                this.ReadExtend(collectionFreePages, extendID, pagesBytes);
            }
        }
    }

    /// <summary>
    /// Read a single extend with 8 pages in 3 bytes. Add pageID into collectionFreePages
    /// </summary>
    private void ReadExtend(CollectionFreePages[] collectionFreePages, int extendID, Span<byte> pageBytes)
    {

    }


    public bool CreateNewExtend(byte colID)
    {
        return true;
    }
}
