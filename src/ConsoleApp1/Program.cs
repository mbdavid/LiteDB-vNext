global using static LiteDB.Constants;
global using static LiteDB.BsonExpression;
global using LiteDB;
global using LiteDB.Engine;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Bogus;
using Bogus.DataSets;

ObjectId a = ObjectId.NewObjectId();



Bogus.Randomizer.Seed = new Random(420);
Bogus.Randomizer.Seed = new Random(420);

var filename = @"C:\temp\test-03.db";

File.Delete(filename);

var settings = new EngineSettings
{
    Filename = filename,
    Timeout = TimeSpan.FromSeconds(50),
};

var db = new LiteEngine(settings);

await db.OpenAsync();

//for(var i = 0; i < 200; i++)
{
    await db.CreateCollectionAsync("col1");
}

await db.InsertAsync("col1", GetData(1, 500), BsonAutoId.Int32);

await db.DeleteAsync("col1", Enumerable.Range(5, 100).Select(x => new BsonInt32(x)).ToArray());

await db.InsertAsync("col1", GetData(10, 50), BsonAutoId.Int32);


//await db.ShutdownAsync();
//
//await db.OpenAsync();
//


//
var cursor = db.Query("col1", new Query());
PrintResult(await db.FetchAsync(cursor, 100));
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

    for(var i = start; i <= end; i++)
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