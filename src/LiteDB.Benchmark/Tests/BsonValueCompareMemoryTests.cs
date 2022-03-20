namespace LiteDB.Benchmark;

[RPlotExporter]
[MemoryDiagnoser]
public class BsonValueCompareMemoryTests
{
    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public int BsonValue_Int() => (BsonValue)10;
}
