global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Running;

using LiteDB;
using LiteDB.Benchmark;

BsonValue a = DateTime.Today;
BsonValue b = "oi";

BsonValue c = a + b;

Console.WriteLine(c.ToString());
Console.WriteLine(c.Type);

// Run<BsonValueCompareTests>();
//BenchmarkRunner.Run<BsonValueCompareTests>();