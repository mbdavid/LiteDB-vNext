//extern alias LiteDBv5;

//global using v5 = LiteDBv5::LiteDB;

global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Running;

using LiteDB;
using LiteDB.Benchmark;

using System.Diagnostics;

try
{
    var a = BsonValue.MaxValue!;
    var b = BsonValue.MinValue!;

    var c = Object.ReferenceEquals(a, b);

    

    Console.WriteLine(c.ToString());

}
catch (Exception ex)
{
    Console.WriteLine(ex.Demystify().ToString());
    //Console.WriteLine("-----------------------------------------");
    //Console.WriteLine(ex.ToString());
}


// Run<BsonValueCompareTests>();
//BenchmarkRunner.Run<BsonExpressionTests>();