namespace LiteDB.Benchmark;

[RPlotExporter]
[MemoryDiagnoser]
public class AutoPropertyCompareTests
{
    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public void AutoProperty() => new AutoPropertyClass();

    [Benchmark]
    public void FieldProperty() => new FieldPropertyClass();
}

public class AutoPropertyClass
{
    public int MyInt { get; set; } = 5;

    public int MyResult { get; set; } = 8;

    public void Test()
    {
        MyResult = MyInt + 10;
    }
}

public class FieldPropertyClass
{
    private int _myInt = 5;
    private int _myResult = 8;

    public int MyInt => _myInt;

    public int MyResult => _myResult;

    public void Test()
    {
        _myResult = _myInt + 10;
    }
}
