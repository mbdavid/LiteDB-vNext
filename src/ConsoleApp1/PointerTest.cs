using LiteDB.Engine;

unsafe
{
    var mf = new MemoryFactory();

    var pg1Ptr = mf.AllocateNewPage();

    Console.WriteLine(pg1Ptr->UniqueID);
    Console.WriteLine(pg1Ptr->PageID);

    pg1Ptr->PageID = 25;

    mf.DeallocatePage(pg1Ptr);

    var pg2Ptr = mf.AllocateNewPage();

    Console.WriteLine(pg2Ptr->UniqueID);
    Console.WriteLine(pg2Ptr->PageID);

    pg2Ptr->PageID = 25;

    mf.DeallocatePage(pg2Ptr);
}

