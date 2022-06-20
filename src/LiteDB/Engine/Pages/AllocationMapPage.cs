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

    private readonly Queue<int> _emptyExtends = new();

    /// <summary>
    /// Create new AllocationMapPage instance
    /// </summary>
    public AllocationMapPage(uint pageID)
        : base(pageID, PageType.AllocationMap)
    {
    }

    /// <summary>
    /// </summary>
    public AllocationMapPage(IMemoryOwner<byte> buffer)
        : base(buffer)
    {
        var span = _readBuffer.Memory.Span;

        for(var i = 0; i < AMP_EXTEND_COUNT; i++)
        {
            var position = PAGE_HEADER_SIZE + (i * AMP_EXTEND_SIZE);

            // check if empty colID (means 
            if (span[position] == 0)
            {
                DEBUG(span[position..(position + AMP_EXTEND_SIZE)].IsFullZero(), "all page allocation map should be empty");

                _emptyExtends.Enqueue(i);
            }
        }
    }

    /// <summary>
    /// Update
    /// </summary>
    public void UpdateMap(uint pageID, PageType pageType, byte colID, ushort freeSpace)
    {
        this.InitializeWrite();
        // usado no foreach depois de salvar em disco as paginas
    }

    public uint GetFreePageID(byte coldID, PageType type, int length)
    {
        this.InitializeWrite();

        return 0;
    }

    public uint NewPageID(byte colID, PageType type)
    {
        this.InitializeWrite();



        return 0;
    }

    private int AddExtend(byte colID)
    {
        var span = _writeBuffer.Memory.Span;

        ENSURE(_emptyExtends.Count > 0, "must have at least 1 empty extend on map page");

        var extendID = _emptyExtends.Dequeue();
        var position = AMP_EXTEND_SIZE; //TODO:sombrio, calcular

        span[position] = colID;

        return extendID;
    }

    #region Static Helpers

    public static bool IsAllocationMapPageID(uint pageID)
    {
        var pfsId = pageID - AMP_FIRST_PAGE_ID;

        return pfsId % AMP_STEP_SIZE == 0;
    }

    public static void GetLocation(uint pageID,
        out uint pfsPageID, // AllocationMapID (começa em 0, 1, 2, 3)
        out int extendIndex, // ExtendID (começa em 0, 1, ..., 1631, 1632, 1633, ...)
        out int pageIndex) // PageIndex inside extend content (0, 1, 2, 3, 4, 5, 6, 7)
    {
        // test if is non-mapped page in PFS
        if (pageID <= AMP_FIRST_PAGE_ID || IsAllocationMapPageID(pageID))
        {
            pfsPageID = uint.MaxValue;
            extendIndex = -1;
            pageIndex = -1;

            return;
        }

        var pfsId = pageID - AMP_FIRST_PAGE_ID;
        var aux = pfsId - (pfsId / AMP_STEP_SIZE + 1);

        pfsPageID = pfsId / AMP_STEP_SIZE * AMP_STEP_SIZE + AMP_FIRST_PAGE_ID;
        extendIndex = (int)(aux / AMP_EXTEND_SIZE) % (PAGE_CONTENT_SIZE / AMP_BYTES_PER_EXTEND);
        pageIndex = (int)(aux % AMP_EXTEND_SIZE);

        ENSURE(IsAllocationMapPageID(pfsPageID), $"Page {pfsPageID} is not a valid PFS");
        ENSURE(extendIndex < AMP_EXTEND_COUNT, $"Extend {extendIndex} must be less than {AMP_EXTEND_COUNT}");
        ENSURE(pageIndex < AMP_EXTEND_SIZE, $"Page index {pageIndex} must be less than {AMP_EXTEND_SIZE}");
    }

    #endregion

}
