global using LiteDB;
global using LiteDB.Engine;

using System.Diagnostics;

using Bogus;
using Bogus.DataSets;

// SETUP
const string VER = "v6";

var INSERT_1 = new Range(1, 300_000);
var DELETE_1 = new Range(5, 60_000);
var INSERT_2 = new Range(6, 30_000);
////////////////////////

Bogus.Randomizer.Seed = new Random(420);

var filename = @$"C:\temp\{VER}\test-{DateTime.Now.Ticks}.db";

var settings = new EngineSettings
{
    Filename = filename,
};

Console.WriteLine($"Filename: {filename} ");

var data1 = GetData(INSERT_1, 200);
var data2 = GetData(INSERT_2, 60);

var initMemory = GC.GetTotalAllocatedBytes();
var sw = Stopwatch.StartNew();

// abre o banco e inicializa
var db = await RunAsync("Create new database", async () =>
{
    var instance = new LiteEngine(settings);

    await instance.OpenAsync();

    return instance;
});

await Run($"Insert {data1.Length}", async () =>
{
    await db.CreateCollectionAsync("col1");

    await db.InsertAsync("col1", data1, BsonAutoId.Int32);

//    await db.CheckpointAsync();
});


await Run($"EnsureIndex (age)", async () =>
{
    await db.EnsureIndexAsync("col1", "idx_age", "age", false);

    //    await db.CheckpointAsync();
});

await Run($"Delete ({DELETE_1})", async () =>
{
    await db.DeleteAsync("col1", Enumerable.Range(DELETE_1.Start.Value, DELETE_1.End.Value).Select(x => new BsonInt32(x)).ToArray());

//    await db.CheckpointAsync();
});

await Run($"Insert {data2.Length}", async () =>
{
    await db.InsertAsync("col1", data2, BsonAutoId.Int32);

});

await Run("Shutdown", async () =>
{
    await db.ShutdownAsync();
});

Console.WriteLine($"-------------");
var usedMemory = GC.GetTotalAllocatedBytes() - initMemory;
var fileLength = new FileInfo(filename).Length;
Console.WriteLine($"FileLength: {(fileLength / 1024L / 1024L):n0} MB ({fileLength:n0} bytes)");
Console.WriteLine($"Total memory used: {usedMemory / 1024L / 1024L:n0} MB ({usedMemory:n0} bytes)");
Console.WriteLine($"Total time: {sw.ElapsedMilliseconds:n0}ms");
Console.WriteLine($"-------------");


#if DEBUG
Console.WriteLine($"# DEBUG - {VER}");
#else
Console.WriteLine($"# RELEASE - {VER}");
#endif

//Console.ReadKey();

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

BsonDocument[] GetData(Range range, int lorem = 5)
{
    return RunSync($"Create dataset ({range}, {lorem})", () =>
    {
        var faker = new Faker();
        var result = new List<BsonDocument>();

        for (var i = range.Start.Value; i <= range.End.Value; i++)
        {
            result.Add(new BsonDocument
            {
                ["_id"] = i,
                ["name"] = faker.Name.FullName(),
                ["age"] = faker.Random.Number(15, 80),
                ["lorem"] = lorem == 0 ? BsonValue.Null : faker.Lorem.Sentence(lorem)
            });
        }

        return result.ToArray();
    });
}

async Task Run(string message, Func<Task> asyncFunc)
{
    var sw = Stopwatch.StartNew();

    Console.Write((" > " + message + "... ").PadRight(40, ' '));

    await asyncFunc();

    Console.WriteLine($": {sw.Elapsed.TotalMilliseconds:n0}ms");

    Console.ForegroundColor = ConsoleColor.Green;
    //db.DumpState();
    Console.ForegroundColor = ConsoleColor.Gray;
}

async Task<T> RunAsync<T>(string message, Func<Task<T>> asyncFunc)
{
    var sw = Stopwatch.StartNew();

    Console.Write((" > " + message + "... ").PadRight(40, ' '));

    var result = await asyncFunc();

    Console.WriteLine($": {sw.Elapsed.TotalMilliseconds:n0}ms");

    return result;
}

T RunSync<T>(string message, Func<T> syncFunc)
{
    var sw = Stopwatch.StartNew();

    Console.Write((" > " + message + "... ").PadRight(40, ' '));

    var result = syncFunc();

    Console.WriteLine($": {sw.Elapsed.TotalMilliseconds:n0}ms");

    return result;
}