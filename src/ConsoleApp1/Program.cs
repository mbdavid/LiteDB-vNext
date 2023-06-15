global using static LiteDB.Constants;
global using LiteDB;
global using LiteDB.Engine;
using System.ComponentModel.DataAnnotations;

var path = @"C:\Temp\litedb-v6-002.db";

File.Delete(path);

var settings = new EngineSettings
{
    Filename = path
};

var doc0 = new BsonDocument { ["name"] = "John Doe", ["age"] = 42 };
var doc1 = new BsonDocument { ["name"] = "Matheus Doe", ["age"] = 33 };

using (var engine = new LiteEngine(settings))
{
    await engine.OpenAsync();

    var created = await engine.CreateCollectionAsync("col1");

    await engine.InsertAsync("col1", new BsonDocument[] { doc0 }, BsonAutoId.Int32);

    await engine.CheckpointAsync();

    await engine.ShutdownAsync();
}




Console.WriteLine("End");
