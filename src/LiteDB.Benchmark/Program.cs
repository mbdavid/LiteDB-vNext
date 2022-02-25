// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;

using LiteDB.Benchmark.BDocument;
using LiteDB.Benchmark.Tests;

//IBValue a = new BInt(1);
//IBValue b = new BInt(10);
//
//Console.WriteLine(a.CompareTo(b));


IBValue a = new BInt(1);


//BenchmarkRunner.Run<BsonValueCompareTests>();
BenchmarkRunner.Run<BsonValueCompareMemoryTests>();