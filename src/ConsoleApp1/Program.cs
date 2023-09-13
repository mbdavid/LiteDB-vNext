// SETUP //////////////////
const string VER = "v6-pointer";
//var INSERT_1 = new Range(1, 300_000);
//var DELETE_1 = new Range(5, 60_000);
//var INSERT_2 = new Range(6, 30_000);
var INSERT_1 = new Range(1, 30_000);
var DELETE_1 = new Range(5, 60_000);
var INSERT_2 = new Range(6, 3_000);
////////////////////////

// DataSet
var insert1 = GetData(INSERT_1, 10);//.ToArray();
var insert2 = GetData(INSERT_2, 6000);///.ToArray();
var delete1 = Enumerable.Range(DELETE_1.Start.Value, DELETE_1.End.Value).Select(x => new BsonInt32(x)).ToArray();
var query1 = new Query { };

var filename = @$"C:\LiteDB\temp\{VER}\test-{DateTime.Now.Ticks}.db";
var settings = new EngineSettings { Filename = filename };

Console.WriteLine($"Filename: {filename} ");

var db = await RunAsync("Create new database", async () =>
{
    var instance = new LiteEngine(settings);
    await instance.OpenAsync();
    return instance;
});

await Run($"Create Collection 'col1'", () => db.CreateCollectionAsync("col1"));
await Run($"Insert {INSERT_1}", () => db.InsertAsync("col1", insert1, BsonAutoId.Int32));
await Run($"Query full 'col1'", () => db.ConsumeAsync(db.Query("col1", query1), 1_000));
await Run($"EnsureIndex (age)", () => db.EnsureIndexAsync("col1", "idx_age", "age", false));
await Run($"Delete ({DELETE_1})", () => db.DeleteAsync("col1", delete1));
await Run($"Insert {INSERT_2}", () => db.InsertAsync("col1", insert2, BsonAutoId.Int32));
await Run("Checkpoint", () => db.CheckpointAsync());
await Run("Shutdown", () => db.ShutdownAsync());

db.Dispose();

Console.WriteLine();
Profiler.PrintResults(filename);

#if DEBUG
Console.WriteLine($"# DEBUG - {VER}");
#else
Console.WriteLine($"# RELEASE - {VER}");
#endif


//Console.ReadKey();
