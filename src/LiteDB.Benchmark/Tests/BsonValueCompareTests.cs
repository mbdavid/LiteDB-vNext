namespace LiteDB.Benchmark;

[RPlotExporter]
[MemoryDiagnoser]
public class BsonValueCompareTests
{
    public int _nativeIntA = 1;
    public decimal _nativeDecimalB = 39m;

    public BsonValue _bsonIntA = 1;
    public BsonValue _bsonDecimalB = 39m;

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public int NativeInt_Int() => _nativeIntA.CompareTo(_nativeDecimalB);

    [Benchmark]
    public int BsonValue_Int() => _bsonIntA.CompareTo(_bsonDecimalB);
}
