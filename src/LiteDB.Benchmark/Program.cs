global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Running;

using LiteDB;
using LiteDB.Benchmark;

using System.Diagnostics;

try
{
    var a = new BsonDocument { ["_id"] = 1, ["name"] = "John" };
    var b = new BsonDocument { ["_id"] = 1, ["name"] = "John" };

    var c = a == b;

    Console.WriteLine(c.ToString());

}
catch (Exception ex)
{
    Console.WriteLine(ex.Demystify().ToString());
    //Console.WriteLine("-----------------------------------------");
    //Console.WriteLine(ex.ToString());
}


// Run<BsonValueCompareTests>();
//BenchmarkRunner.Run<AutoPropertyCompareTests>();