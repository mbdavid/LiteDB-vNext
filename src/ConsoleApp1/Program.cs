global using static LiteDB.Constants;
global using LiteDB;
global using LiteDB.Engine;


var path = @"C:\Temp\litedb-v6-001.db";

File.Delete(path);

var settings = new EngineSettings
{
    Filename = path
};

using (var engine = new LiteEngine(settings))
{
    var opened = await engine.OpenAsync();

    var created = await engine.CreateCollectionAsync("col1");

    await engine.InsertAsync("col1", new BsonDocument());

    await engine.CheckpointAsync();
}


Console.WriteLine("End");