//extern alias LiteDBv5;

//global using v5 = LiteDBv5::LiteDB;

global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Running;

using LiteDB;
using LiteDB.Benchmark;

using System.Diagnostics;

try
{
    BsonValue a = 100;
    BsonValue b = 200;

    var c = a + b;

    

    Console.WriteLine(c.ToString());

}
catch (Exception ex)
{
    Console.WriteLine(ex.Demystify().ToString());
    //Console.WriteLine("-----------------------------------------");
    //Console.WriteLine(ex.ToString());
}


// Run<BsonValueCompareTests>();
BenchmarkRunner.Run<BsonExpressionTests>();