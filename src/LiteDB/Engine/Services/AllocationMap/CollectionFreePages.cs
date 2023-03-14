namespace LiteDB.Engine;

/// <summary>
/// A single structure to hold a list of all pagesID inside a AllocationMap page 
/// </summary>
internal struct CollectionFreePages
{
    //TODO: test if hashset is the fast/best option here

    public HashSet<uint> EmptyPages;

    public HashSet<uint> DataPages_001; // 100% empty
    public HashSet<uint> DataPages_010;
    public HashSet<uint> DataPages_011;
    public HashSet<uint> DataPages_100; // full ???? DEVE SAIR? Provavel

    public HashSet<uint> IndexPages_100; // 100% empty
    public HashSet<uint> IndexPages_110;
    public HashSet<uint> IndexPages_111; // full ???? DEVE SAIR? Provavel

    public CollectionFreePages()
    {
        this.EmptyPages = new();

        this.DataPages_001 = new();
        this.DataPages_010 = new();
        this.DataPages_100 = new();    
        this.DataPages_011 = new();

        this.IndexPages_100 = new();
        this.IndexPages_110 = new();
        this.IndexPages_111 = new();
    }
}