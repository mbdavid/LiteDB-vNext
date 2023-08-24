global using static LiteDB.Constants;
global using static LiteDB.BsonExpression;
global using LiteDB;
global using LiteDB.Engine;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Bogus;
using Bogus.DataSets;

Bogus.Randomizer.Seed = new Random(420);

var filename = @"C:\temp\test-03.db";

File.Delete(filename);

var settings = new EngineSettings
{
    Filename = filename,
    Timeout = TimeSpan.FromSeconds(50),
};


//var data1 = GetData(1, 100_000, 200);
//var data2 = GetData(10, 50, 6000);
var data1 = GetData(1, 100, 20);
var data2 = GetData(10, 50, 600);

//Console.ReadKey();

var initMemory = GC.GetTotalAllocatedBytes();
var sw = Stopwatch.StartNew();

// abre o banco e inicializa
var db = new LiteEngine(settings);

await db.OpenAsync();

await db.CreateCollectionAsync("col1");


// Executa operações

await db.InsertAsync("col1", data1, BsonAutoId.Int32);


await db.EnsureIndexAsync("col1", "idx_age", "age", false);


await db.DeleteAsync("col1", Enumerable.Range(1, 50).Select(x => new BsonInt32(x)).ToArray());
//
await db.InsertAsync("col1", data2, BsonAutoId.Int32);


//var cursor = db.Query("col1", new AggregateQuery
//{
//    Functions = new IAggregateFunc[] 
//    {
//        new CountFunc("total", "$")
//    }
//});
//PrintResult(await db.FetchAsync(cursor, 100));

//db.DumpMemory();

var usedMemory = GC.GetTotalAllocatedBytes() - initMemory;

Console.WriteLine($"Time: {sw.Elapsed.TotalMilliseconds:n0}ms");

//var cursor = db.Query("col1", new Query { Select = "{_id,name,len:length(lorem)}", OrderBy = new ("name", 1) });
//PrintResult(await db.FetchAsync(cursor, 100));


Console.WriteLine("  > Starting shutdown()...");
var swShutdown = Stopwatch.StartNew();

await db.ShutdownAsync();

Console.WriteLine($"  > Total time to shutdown: {swShutdown.Elapsed.TotalMilliseconds:n0}ms");

Console.WriteLine($"Total memory used: {usedMemory} - {usedMemory / 1024:n0}K");
Console.WriteLine($"Total time: {sw.Elapsed.TotalMilliseconds:n0}ms");

//Console.ReadKey();
return;


await db.OpenAsync();



//
//var cursor = db.Query("col1", new Query { Select = "{_id,name,len:length(lorem)}" });
//PrintResult(await db.FetchAsync(cursor, 100));
//PrintResult(await db.FetchAsync(cursor, 100));
//PrintResult(await db.FetchAsync(cursor, 100));

//var um = await db.FindById("col1", 1, Array.Empty<string>());
//var dois = await db.FindById("col1", 2, Array.Empty<string>());

await db.Dump(0);

await db.ShutdownAsync();

return;

await db.OpenAsync();

var um = await db.FindById("col1", 1, Array.Empty<string>());

//await db.InsertAsync("col1", GetData(10_000, 0), BsonAutoId.Int32);


var cursor2 = db.Query("col1", new Query());
PrintResult(await db.FetchAsync(cursor2, 100));


Console.WriteLine("\n\nEnd");
Console.ReadKey();


BsonDocument[] GetData(int start, int end, int lorem = 5)
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
}

void PrintResult(FetchResult result)
{
    Console.WriteLine("Results: " + result);

    foreach (var item in result.Results)
    {
        Console.WriteLine(item.ToString());
    }
}