namespace LiteDB.Engine;

/// <summary>
/// A single structure to hold a list of all pagesID inside a AllocationMap page extends
/// </summary>
internal class CollectionFreePages
{
    //TODO: test if hashset is the fast/best option here

    /// <summary>
    /// Constains a list of empty pages. Can be used by Data or Index [value: 000 = 0]
    /// </summary>
    public FreePageList EmptyPages;

    /// <summary>
    /// Contains a list of free data pages with at least, 91% free [value: 001 = 1]
    /// </summary>
    public FreePageList DataPagesLarge;

    /// <summary>
    /// Contains a list of free data pages with space beteen 51% - 90% [value: 010 = 2]
    /// </summary>
    public FreePageList DataPagesMedium;

    /// <summary>
    /// Contains a list of free data pages with space between 31% - 50% [value: 011 = 3]
    /// </summary>
    public FreePageList DataPagesSmall;

    /// <summary>
    /// Contains a list of free index pages with space avaiable to at least one more node [value: 101 = 5]
    /// </summary>
    public FreePageList IndexPages;

    public CollectionFreePages()
    {
        this.EmptyPages = new();

        this.DataPagesLarge = new();
        this.DataPagesMedium = new();
        this.DataPagesSmall = new();

        this.IndexPages = new();
    }
}