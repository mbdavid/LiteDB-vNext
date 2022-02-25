// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;

using LiteDB.Benchmark.BDocument;
using LiteDB.Benchmark.Tests;

XValue a = new XValue(1);
XValue b = new XValue(10);

//Console.WriteLine(a.CompareTo(b));


//BenchmarkRunner.Run<BsonValueCompareTests>();
BenchmarkRunner.Run<BsonValueCompareMemoryTests>();