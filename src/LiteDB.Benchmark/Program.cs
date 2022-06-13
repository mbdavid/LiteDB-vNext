//extern alias LiteDBv5;

//global using v5 = LiteDBv5::LiteDB;

global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Running;

using LiteDB;
using LiteDB.Benchmark;

using System.Diagnostics;

BenchmarkRunner.Run<BsonExpressionTests>();