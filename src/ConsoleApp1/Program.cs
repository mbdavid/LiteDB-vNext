global using LiteDB;
global using LiteDB.Engine;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// SETUP
const string VER = "v6-pointer";


//var INSERT_1 = new Range(1, 300_000);
//var DELETE_1 = new Range(5, 60_000);
//var INSERT_2 = new Range(6, 30_000);
var INSERT_1 = new Range(1, 1_000);
//var DELETE_1 = new Range(5, 600);
//var INSERT_2 = new Range(6, 300);
////////////////////////

var _random = new Random(420);

var filename = @$"C:\LiteDB\temp\{VER}\test-{DateTime.Now.Ticks}.db";

var settings = new EngineSettings
{
    Filename = filename,
};

Console.WriteLine($"Filename: {filename} ");

var data1 = GetData(INSERT_1, 200).ToArray();
//var data2 = GetData(INSERT_2, 60).ToArray();

//await Task.Delay(5_000); Console.WriteLine("Initializing...");

var sw = Stopwatch.StartNew();

// abre o banco e inicializa
var db = await RunAsync("Create new database", async () =>
{
    var instance = new LiteEngine(settings);

    await instance.OpenAsync();

    return instance;
});

await Run($"Create Collection 'col1'", async () =>
{
    await db.CreateCollectionAsync("col1");
});

Profiler.Reset();

await Run($"Insert {INSERT_1}", async () =>
{
    await db.InsertAsync("col1", data1, BsonAutoId.Int32);
});

Profiler.AddResult("Insert", true);

await Run("Checkpoint", async () =>
{
    await db.CheckpointAsync();
});

await Run("Shutdown", async () =>
{
    await db.ShutdownAsync();
});

//await Run("Re-open database", async () =>
//{
//    await db.OpenAsync();
//});
//
//Profiler.Reset();
//
//await Run($"Query full 'col1'", async () =>
//{
//    await ConsumeAsync(db, db.Query("col1", new Query { }), 1_000);
//});
//
//Profiler.AddResult("Query", true);
//
//
//await Run($"EnsureIndex (age)", async () =>
//{
//    await db.EnsureIndexAsync("col1", "idx_age", "age", false);
//});
//
//Profiler.AddResult("EnsureIndex", true);
//
//
//await Run($"Delete ({DELETE_1})", async () =>
//{
//    await db.DeleteAsync("col1", Enumerable.Range(DELETE_1.Start.Value, DELETE_1.End.Value).Select(x => new BsonInt32(x)).ToArray());
//});
//
//await Run($"Insert {INSERT_2}", async () =>
//{
//    await db.InsertAsync("col1", data2, BsonAutoId.Int32);
//});
//
//await Run("Checkpoint", async () =>
//{
//    await db.CheckpointAsync();
//});
//
//await Run("Shutdown", async () =>
//{
//    await db.ShutdownAsync();
//});

Console.WriteLine($"-------------");
var fileLength = new FileInfo(filename).Length;
Console.WriteLine($"FileLength: {(fileLength / 1024L / 1024L):n0} MB ({fileLength:n0} bytes)");
Console.WriteLine($"Total time: {sw.ElapsedMilliseconds:n0}ms");
Console.WriteLine($"-------------");

Profiler.PrintResults();

#if DEBUG
Console.WriteLine($"# DEBUG - {VER}");
#else
Console.WriteLine($"# RELEASE - {VER}");
#endif



Console.ReadKey();

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

IEnumerable<BsonDocument> GetData(Range range, int lorem = 5)
{
    for (var i = range.Start.Value; i <= range.End.Value; i++)
    {
        var doc = new BsonDocument
        {
            ["_id"] = i,
            ["name"] = Faker.Fullname(),
            ["age"] = Faker.Age(),
            ["lorem"] = Faker.Lorem(lorem),
            ["padding"] = ""
        };

        var len = doc.GetBytesCount();
        var padding = (len % 8) > 0 ? 8 - (len % 8) : 0;

        if (padding>0)
        {
            doc["padding"] = "".PadLeft(padding, '0');
        }


        yield return doc;
    }
}

async Task Run(string message, Func<Task> asyncFunc)
{
    var sw = Stopwatch.StartNew();

    Console.Write((" > " + message + "... ").PadRight(40, ' ') + ": ");

    await asyncFunc();

    Console.WriteLine($"{sw.Elapsed.TotalMilliseconds:n0}ms");

    Console.ForegroundColor = ConsoleColor.Green;
    //db.DumpState();
    Console.ForegroundColor = ConsoleColor.Gray;
}

async Task ConsumeAsync(ILiteEngine db, Guid cursorID, int fetchSize)
{
    var result = await db.FetchAsync(cursorID, fetchSize);
    var total = result.FetchCount;

    while (result.HasMore)
    {
        result = await db.FetchAsync(cursorID, fetchSize);
        total += result.FetchCount;
    }

    Console.ForegroundColor = ConsoleColor.DarkBlue;
    Console.Write($"[{total}] ");
    Console.ForegroundColor = ConsoleColor.Gray;
}

async Task<T> RunAsync<T>(string message, Func<Task<T>> asyncFunc)
{
    var sw = Stopwatch.StartNew();

    Console.Write((" > " + message + "... ").PadRight(40, ' ') + ": ");

    var result = await asyncFunc();

    Console.WriteLine($"{sw.Elapsed.TotalMilliseconds:n0}ms");

    return result;
}

T RunSync<T>(string message, Func<T> syncFunc)
{
    var sw = Stopwatch.StartNew();

    Console.Write((" > " + message + "... ").PadRight(40, ' ') + ": ");

    var result = syncFunc();

    Console.WriteLine($"{sw.Elapsed.TotalMilliseconds:n0}ms");

    return result;
}