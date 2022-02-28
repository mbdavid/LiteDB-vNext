// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;

using LiteDB.Benchmark.BDocument;
using LiteDB.Benchmark.Tests;

//IBValue a = (BInt)1;
//IBValue b = (BInt)10;
//
//Console.WriteLine(a.CompareTo(b));


BenchmarkRunner.Run<BsonValueCompareTests>();
//BenchmarkRunner.Run<BsonValueCompareMemoryTests>();