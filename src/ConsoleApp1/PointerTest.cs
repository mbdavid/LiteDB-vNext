using LiteDB;
using LiteDB.Engine;

unsafe
{
    var s = sizeof(IndexKey2);

    var asBool_0 = IndexKey2.AllocNewIndexKey(false);
    var asBool_1 = IndexKey2.AllocNewIndexKey(true);

    var asInt32 = IndexKey2.AllocNewIndexKey(7737);
    var asInt64 = IndexKey2.AllocNewIndexKey(7737L);


    var asString_1 = IndexKey2.AllocNewIndexKey("Mauricio David");

    Console.WriteLine(asString_1->ToString());





}
return;



// ------------------------------------------------------------------------------------------------------
// ------------------------------------------------------------------------------------------------------
// ------------------------------------------------------------------------------------------------------

var filename = @$"C:\LiteDB\temp\v6\test-pointer-{DateTime.Now.Ticks}.db";

var settings = new EngineSettings
{
    Filename = filename,
};

var db = new LiteEngine(settings);

await db.OpenAsync();

await db.CreateCollectionAsync("col1");


for (var i = 420; i < 10_000; i++)
{
    // 64 bytes doc
    await db.InsertAsync("col1", new BsonDocument[] { new BsonDocument() { ["_id"] = i, ["long_name"] = "dkhfsdkjhsdkjfhskfshsdkfhsdkjhdskjfhfksjdh" } }, BsonAutoId.Int32);

}

await db.ShutdownAsync();

db.DumpState();


return;

