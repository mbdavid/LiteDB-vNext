global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Running;

using LiteDB;
using LiteDB.Benchmark;

using System.Buffers;
using System.Diagnostics;


var mem = MemoryPool<byte>.Shared.Rent(8192);

mem.Memory.Span[5] = 1;

mem.Memory.Span[8] = 2;

var ab = mem.Memory.Span.Trim<byte>(0);

var newarr = MemoryPool<byte>.Shared.Rent(ab.Length);


ab.CopyTo(newarr.Memory.Span);



Console.WriteLine(ab.ToArray());

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