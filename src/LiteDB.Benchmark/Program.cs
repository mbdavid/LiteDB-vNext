global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Running;

using LiteDB;
using LiteDB.Benchmark;


BsonDocument d = new()
{
    ["_id"] = 1,
    ["name"] = "John",
    ["nulo"] = null,
    ["bool"] = true,
    ["max"] = BsonValue.MaxValue,
    ["arr"] = new BsonArray { 1, "2", null }
};



;
//BenchmarkRunner.Run<BsonValueCompareTests>();
//BenchmarkRunner.Run<BsonValueCompareMemoryTests>();