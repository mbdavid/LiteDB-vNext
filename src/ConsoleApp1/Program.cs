global using static LiteDB.Constants;
global using static LiteDB.BsonExpression;
global using LiteDB;
global using LiteDB.Engine;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Bogus;

Bogus.Randomizer.Seed = new Random(420);

var filename = @"C:\temp\test-03.db";

File.Delete(filename);

var settings = new EngineSettings
{
    Filename = filename
};

var db = new LiteEngine(settings);

var data = GetData(2, 0);

await db.OpenAsync();

await db.CreateCollectionAsync("col1");

await db.InsertAsync("col1", data, BsonAutoId.Int32);

var cursor = db.Query("col1", new Query());

var result = await db.FetchAsync(cursor, 100);


Console.WriteLine("Results: " + result);

foreach (var item in result.Results)
{
    Console.WriteLine(item.ToString());
}

//var um = await db.FindById("col1", 1, Array.Empty<string>());
//var dois = await db.FindById("col1", 2, Array.Empty<string>());


await db.ShutdownAsync();


Console.WriteLine("\n\nEnd");


BsonDocument[] GetData(int count, int lorem)
{
    var faker = new Faker();

    return Enumerable.Range(1, count).Select(x => new BsonDocument
    {
        ["_id"] = x,
        ["name"] = faker.Name.FullName(),
        ["age"] = faker.Random.Number(15, 80),
        ["lorem"] = lorem == 0 ? BsonValue.Null : faker.Lorem.Sentence(lorem)
    }).ToArray();
}
