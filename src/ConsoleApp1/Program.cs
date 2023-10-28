// SETUP //////////////////
const string VER = "v6-pointer";
var INSERT_1 = new Range(1, 10_000);
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

/*
var doc = new BsonDocument
{
    ["_id"] = i,
    ["name"] = Faker.Fullname(),
    ["age"] = Faker.Age(),
    ["created"] = Faker.Birthday(),
    ["country"] = BsonDocument.DbRef(Faker.Next(1, 10), "col2"),
    ["lorem"] = Faker.Lorem(lorem, loremEnd)
};
*/

// RUN 

await db.RunAsync($"Create Collection 'col1'", "CREATE COLLECTION col1");
await db.RunAsync($"Insert col1 {insert1.Length:n0}", "INSERT INTO col1 VALUES @0", insert1);

await db.RunAsync($"EnsureIndex (age)", "CREATE INDEX idx_01 ON col1 ($.age)");
//await db.RunAsync($"EnsureIndex (name)", "CREATE INDEX idx_02 ON col1 (name)");

await db.RunQueryAsync(20, $"Query1",
    @"SELECT COUNT(_id), sum(age) as sum, avg(age) as avg, max(name) as max
        FROM col1");


//await db.RunQueryAsync(20, $"Query1",
//    @"SELECT age, 
//             COUNT(age) AS qtd,
//             --MIN(name) AS min_nome,
//             --MAX(name) AS max_name,
//             --FIRST(name) AS first_nome,
//             --LAST(name) AS last_name,
//             --AVG(DOUBLE(DAY(created))) AS avg_day,
//             --SUM(age) AS sum_age,
//             --ANY(age) AS any_age,
//             --ARRAY(name) as arr_nomes,
//             SUM(age) AS #sum_age,
//             sum_age + 100 AS sum_100
//        FROM col1 
//       GROUP BY age");

// SHUTDOWN
await db.ShutdownAsync();
db.Dispose();

// PRINT
Console.WriteLine();
//Profiler.PrintResults(filename);

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

