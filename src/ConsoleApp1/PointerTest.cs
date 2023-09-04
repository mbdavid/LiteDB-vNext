using LiteDB;
using LiteDB.Engine;

var filename = @$"C:\LiteDB\temp\v6\test-pointer-{DateTime.Now.Ticks}.db";

var settings = new EngineSettings
{
    Filename = filename,
};

var db = new LiteEngine(settings);

await db.OpenAsync();

await db.CreateCollectionAsync("col1");

await db.InsertAsync("col1", new BsonDocument[] { new BsonDocument() { ["_id"] = 420 } }, BsonAutoId.Int32);

await db.ShutdownAsync();

db.DumpState();


return;

unsafe
{
    var mf = new MemoryFactory();

    var pg1 = mf.AllocateNewPage();

    Console.WriteLine(pg1->UniqueID);


    pg1->PositionID = 0b100_0001_0100_0010_0100_0011_0100_0100; // 65,66,67,68

    pg1->Teste();



    mf.DeallocatePage(pg1);

    var pg2 = mf.AllocateNewPage();

    Console.WriteLine(pg2->UniqueID);
    Console.WriteLine(pg2->PageID);

    pg2->PageID = 25;

    mf.DeallocatePage(pg2);
}

