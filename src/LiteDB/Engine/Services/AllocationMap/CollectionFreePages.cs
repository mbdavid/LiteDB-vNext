namespace LiteDB.Engine;

/// <summary>
/// A single structure to hold a list of all pagesID inside a AllocationMap page extends
/// </summary>
internal struct CollectionFreePages
{
    //TODO: test if hashset is the fast/best option here

    public FreePageList EmptyPages;

    public FreePageList DataPages_1;
    public FreePageList DataPages_2;
    public FreePageList DataPages_3;

    public FreePageList IndexPages;

    public CollectionFreePages()
    {
        this.EmptyPages = new();

        this.DataPages_1 = new();
        this.DataPages_2 = new();
        this.DataPages_3 = new();

        this.IndexPages = new();
    }
}