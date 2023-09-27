﻿// SETUP //////////////////
const string VER = "v6-pointer";
var INSERT_1 = new Range(1, 100_000);
var DELETE_1 = new Range(1, 40_000);
var INSERT_2 = new Range(1, 30_000);
////////////////////////

// DATASETS
var insert1 = GetData(INSERT_1, 100, 300).ToArray();
var insert2 = GetData(INSERT_2, 5, 10).ToArray();

var delete1 = Enumerable.Range(DELETE_1.Start.Value, DELETE_1.End.Value).Select(x => new BsonInt32(x)).ToArray();

// INITIALIZE
var filename = @$"C:\LiteDB\temp\{VER}\test-{DateTime.Now.Ticks}.db";
var settings = new EngineSettings { Filename = filename };
var db = new LiteEngine(settings);

// OPEN
await db.OpenAsync();


// RUN 

await db.RunAsync($"Create Collection 'col1'", "CREATE COLLECTION col1");

await db.RunAsync($"Insert col1 {insert1}", "INSERT INTO col1 VALUES @0", BsonArray.FromArray(insert1));


await db.RunAsync($"EnsureIndex (age)", "CREATE INDEX idx_01 ON col1 ($.age)");
await db.RunAsync($"EnsureIndex (name)", "CREATE INDEX idx_02 ON col1 (name)");


await db.RunAsync($"Query1", "SELECT * FROM col1");

// SHUTDOWN
await db.ShutdownAsync();
db.Dispose();

// PRINT
Console.WriteLine();
Profiler.PrintResults(filename);

#if DEBUG
Console.WriteLine($"# DEBUG - {VER}");
#else
Console.WriteLine($"# RELEASE - {VER}");
#endif


//Console.ReadKey();

//unsafe
//{
//    int colID = 1;

//    var buffer = new byte[10];

//    var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
//    var ptr = handle.AddrOfPinnedObject();
//    var myStruct = (MyStruct*)ptr;

//    myStruct->ExtendValue = (int)(colID << 24);

//    Console.WriteLine(myStruct->ExtendValue);

//    for (var i = 0; i < 10; i++)
//    {
//        var v = *(byte*)(ptr);
//        var bin = Convert.ToString(v, 2).PadLeft(8, '0');
//        Console.WriteLine(i + " - " + bin);
//        ptr++;
//    }

//}

//return;

