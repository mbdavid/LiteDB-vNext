namespace LiteDB.Engine;

/// <summary>
/// </summary>
internal class AllocationMapPage : BasePage
{
    public int ExtendsCount => 0;

    private List<short> _emptyExtends = new();

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
    }

    public int AddExtend(int colID)
    {
        this.InitializeWrite();

        // marca no buffer, remove do array de _emptyExtends

        return 0; // retorna a extendID criada (considera o PageID?)
    }

    public void Update(byte colID, uint pageID, PageType pageType, int freeSpace)
    {
        this.InitializeWrite();
        // usado no foreach depois de salvar em disco as paginas
    }

    public uint GetAllocationPage(byte colID, PageType pageType, int length)
    {
        return 0;
    }

    public uint AllocateNewPage(byte colID, PageType pageType)
    {
        return 0;
    }


    #region Static Helpers

    public static bool IsAllocationMapPageID(uint pageID)
    {
        var pfsId = pageID - PFS_FIRST_PAGE_ID;

        return pfsId % PFS_STEP_SIZE == 0;
    }

    public static void GetLocation(uint pageID,
        out uint pfsPageID, // AllocationMapID (começa em 0, 1, 2, 3)
        out int extendIndex, // ExtendID (começa em 0, 1, ..., 1631, 1632, 1633, ...)
        out int pageIndex) // PageIndex inside extend content (0, 1, 2, 3, 4, 5, 6, 7)
    {
        // test if is non-mapped page in PFS
        if (pageID <= PFS_FIRST_PAGE_ID || IsAllocationMapPageID(pageID))
        {
            pfsPageID = uint.MaxValue;
            extendIndex = -1;
            pageIndex = -1;

            return;
        }

        var pfsId = pageID - PFS_FIRST_PAGE_ID;
        var aux = pfsId - (pfsId / PFS_STEP_SIZE + 1);

        pfsPageID = pfsId / PFS_STEP_SIZE * PFS_STEP_SIZE + PFS_FIRST_PAGE_ID;
        extendIndex = (int)(aux / PFS_EXTEND_SIZE) % (PAGE_CONTENT_SIZE / PFS_BYTES_PER_EXTEND);
        pageIndex = (int)(aux % PFS_EXTEND_SIZE);

        ENSURE(IsAllocationMapPageID(pfsPageID), $"Page {pfsPageID} is not a valid PFS");
        ENSURE(extendIndex < PFS_EXTEND_COUNT, $"Extend {extendIndex} must be less than {PFS_EXTEND_COUNT}");
        ENSURE(pageIndex < PFS_EXTEND_SIZE, $"Page index {pageIndex} must be less than {PFS_EXTEND_SIZE}");
    }

    #endregion

}
