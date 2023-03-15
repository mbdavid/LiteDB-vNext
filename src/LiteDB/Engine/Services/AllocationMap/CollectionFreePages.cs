namespace LiteDB.Engine;

/// <summary>
/// A single structure to hold a list of all pagesID inside a AllocationMap page extends
/// </summary>
internal struct CollectionFreePages
{
    //TODO: test if hashset is the fast/best option here

    public HashSet<uint> EmptyPages_0;

    public HashSet<uint> DataPages_1;
    public HashSet<uint> DataPages_2;
    public HashSet<uint> DataPages_3;

    public HashSet<uint> IndexPages;

    public CollectionFreePages()
    {
        this.EmptyPages_0 = new();

        this.DataPages_1 = new();
        this.DataPages_2 = new();
        this.DataPages_3 = new();

        this.IndexPages = new();
    }
}