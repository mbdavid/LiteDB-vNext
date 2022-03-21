global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Running;

using LiteDB;
using LiteDB.Benchmark;


BsonDocument d = new()
{
    ["_id"] = 1,
    ["name"] = "John"
};



;
//BenchmarkRunner.Run<BsonValueCompareTests>();
//BenchmarkRunner.Run<BsonValueCompareMemoryTests>();