global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Running;

using LiteDB;
using LiteDB.Benchmark;

using System.Diagnostics;

try
{
    BsonInt32 a = 10;
    BsonDouble b = 20;

    var c = a + b;

    Console.WriteLine(c.ToString());
    Console.WriteLine(c.Type);

}
catch (Exception ex)
{
    Console.WriteLine(ex.Demystify().ToString());
    //Console.WriteLine("-----------------------------------------");
    //Console.WriteLine(ex.ToString());
}


// Run<BsonValueCompareTests>();
BenchmarkRunner.Run<AutoPropertyCompareTests>();