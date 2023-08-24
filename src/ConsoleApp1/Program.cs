global using LiteDB;
global using LiteDB.Engine;
using System.Diagnostics;
using Bogus;
using Bogus.DataSets;

// SETUP
const string VER = "v6";

////////////////////////

Bogus.Randomizer.Seed = new Random(420);

var filename = @$"C:\temp\{VER}\test-{DateTime.Now.Ticks}.db";

var settings = new EngineSettings
{
    Filename = filename,
};

#if DEBUG
Console.WriteLine($"# DEBUG - {VER}");
#else
Console.WriteLine($"# RELEASE - {VER}");
#endif

Console.WriteLine($"Filename: {filename} ");

var data1 = GetData(1, 100_000, 200);
var data2 = GetData(10, 50, 6000);

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
});

await Run($"EnsureIndex (age)", async () =>
{
    await db.EnsureIndexAsync("col1", "idx_age", "age", false);
});

await Run($"Delete (1-50)", async () =>
{
    await db.DeleteAsync("col1", Enumerable.Range(1, 50).Select(x => new BsonInt32(x)).ToArray());
});

await Run($"Insert {data2.Length}", async () =>
{
    await db.InsertAsync("col1", data2, BsonAutoId.Int32);
});

await Run("Shutdown", async () =>
{
    await db.ShutdownAsync();
    db.Dispose();
});

Console.WriteLine($"-------------");
var usedMemory = GC.GetTotalAllocatedBytes() - initMemory;
Console.WriteLine($"FileLength: {(new FileInfo(filename).Length/1024/1024):n0} MB ({new FileInfo(filename).Length:n0} bytes)");

Console.WriteLine($"Total memory used: {usedMemory:n0} bytes");
Console.WriteLine($"Total time: {sw.ElapsedMilliseconds:n0}ms");
Console.WriteLine($"-------------");


db.DumpMemory();

//Console.ReadKey();

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

BsonDocument[] GetData(int start, int end, int lorem = 5)
{
    return RunSync($"Create dataset ({start}, {end}, {lorem})", () =>
    {
        var faker = new Faker();
        var result = new List<BsonDocument>();

        for (var i = start; i <= end; i++)
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