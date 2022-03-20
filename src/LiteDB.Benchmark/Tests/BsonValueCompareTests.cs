namespace LiteDB.Benchmark;

[RPlotExporter]
public class BsonValueCompareTests
{
    public int _nativeIntA = 1;
    public int _nativeIntB = 10;

    public BsonValue _bsonIntA = 1;
    public BsonValue _bsonIntB = 10;

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public int NativeInt_Int() => _nativeIntA.CompareTo(_nativeIntB);

    [Benchmark]
    public int BsonValue_Int() => _bsonIntA.CompareTo(_bsonIntB);
}
