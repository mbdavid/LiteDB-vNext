using LiteDB;
using LiteDB.Engine;

unsafe
{
    var s = sizeof(IndexKey2);

    // 8 bytes
    var asNull = IndexKey2.AllocNewIndexKey(BsonValue.Null);
    var asMinValue = IndexKey2.AllocNewIndexKey(BsonValue.MaxValue);
    var asMaxValue = IndexKey2.AllocNewIndexKey(BsonValue.MinValue);

    var asBool_0 = IndexKey2.AllocNewIndexKey(false);
    var asBool_1 = IndexKey2.AllocNewIndexKey(true);

    var asInt32 = IndexKey2.AllocNewIndexKey(7737);
    var asInt64 = IndexKey2.AllocNewIndexKey(7737L);

    // 16 bytes
    var asDouble = IndexKey2.AllocNewIndexKey(7737d);
    var asDecimal = IndexKey2.AllocNewIndexKey(7737m);

    var asDateTime = IndexKey2.AllocNewIndexKey(DateTime.Now);

    // 24 bytes
    var asGuid = IndexKey2.AllocNewIndexKey(Guid.NewGuid());
    var asObjectId = IndexKey2.AllocNewIndexKey(ObjectId.NewObjectId());


    var asString_1 = IndexKey2.AllocNewIndexKey("Less_8");
    var asString_2 = IndexKey2.AllocNewIndexKey("Between_8-16");
    var asString_3 = IndexKey2.AllocNewIndexKey("Larger_than_24_bytes_here");


    var asBinary_1 = IndexKey2.AllocNewIndexKey(new byte[50]);


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

