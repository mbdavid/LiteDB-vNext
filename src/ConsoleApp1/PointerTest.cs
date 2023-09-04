//using LiteDB;
//using LiteDB.Engine;


//var filename = @$"C:\LiteDB\temp\v6\test-pointer-{DateTime.Now.Ticks}.db";

//var settings = new EngineSettings
//{
//    Filename = filename,
//};

//var db = new LiteEngine(settings);

//await db.OpenAsync();

//await db.CreateCollectionAsync("col1");


//for(var i = 420; i < 10_000; i++)
//{
//    // 64 bytes doc
//    await db.InsertAsync("col1", new BsonDocument[] { new BsonDocument() { ["_id"] = i, ["long_name"] = "dkhfsdkjhsdkjfhskfshsdkfhsdkjhdskjfhfksjdh" } }, BsonAutoId.Int32);

//}

//await db.ShutdownAsync();

//db.DumpState();


//return;

//unsafe
//{
//var mem = stackalloc byte[128];

//mem[0] = 5;
//mem[1] = 0;
//mem[2] = 0;
//mem[3] = 0;

//MyStruct1* myS = (MyStruct1*)mem;

//myS->MyInt1 = 9;

//Console.WriteLine($"sizeof(IndexNode)-{sizeof(IndexNode)}");
//Console.WriteLine($"sizeof(IndexNodeLevel)-{sizeof(IndexNodeLevel)}");
//Console.WriteLine($"sizeof(IndexKey)-{sizeof(IndexKey)}");
//Console.WriteLine("-----");

//Console.WriteLine($"sizeof(DataBlock)-{sizeof(DataBlock)}");
//Console.WriteLine($"sizeof(RowID)-{sizeof(RowID)}");

//return;


//var mf = new MemoryFactory();

//var pg1 = mf.AllocateNewPage();

//Console.WriteLine(pg1->UniqueID);


//pg1->PositionID = 0b100_0001_0100_0010_0100_0011_0100_0100; // 65,66,67,68




//mf.DeallocatePage(pg1);

//var pg2 = mf.AllocateNewPage();

//Console.WriteLine(pg2->UniqueID);
//Console.WriteLine(pg2->PageID);

//pg2->PageID = 25;

//mf.DeallocatePage(pg2);


//}


//struct MyStruct1
//{
//    public int MyInt1;  //4
//    public long MyLong1; // 8
//    public int MyInt2; // 4
//}
//struct MyStruct2
//{
//    public long MyLong1; // 8
//    public int MyLong2; // 8
//}

