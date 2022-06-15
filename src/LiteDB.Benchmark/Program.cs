
for(var i = 26000; i < 27000; i++)
{
    DiskService.GetLocation((uint)i, out var pfs, out var ext, out var pid);

    Console.WriteLine($"{i}: ({pfs}:{ext}:{pid})");
}

// Run<BsonValueCompareTests>();
//BenchmarkRunner.Run<BsonExpressionTests>();