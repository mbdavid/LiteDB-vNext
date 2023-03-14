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
    /// List of extends in this page, indexed by colID
    /// </summary>
    private readonly List<ExtendSummaryInfo>[] _extends = new List<ExtendSummaryInfo>[byte.MaxValue];

    /// <summary>
    /// Create new AllocationMapPage instance
    /// </summary>
    public AllocationMapPage(uint pageID, PageBuffer writeBuffer)
        : base(pageID, PageType.AllocationMap, writeBuffer)
    {
        // 
    }

    /// <summary>
    /// </summary>
    public AllocationMapPage(PageBuffer readBuffer, IMemoryCacheService memoryCache)
        : base(readBuffer, memoryCache)
    {
        throw new NotImplementedException();
    }

}
