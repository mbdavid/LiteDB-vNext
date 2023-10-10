namespace LiteDB.Benchmark;

[RPlotExporter]
[MemoryDiagnoser]
public class BsonWriterTests
{
    private static BsonDocument _doc = new ()
    {
        ["_id"] = "John",
        ["name"] = 1.8,
        ["demo"] = 123,
        ["arr"] = new BsonArray { 11, true, BsonNull.Null, Decimal.MaxValue }
    };

    [Benchmark]
    public void SerializeV7()
    {
        var data = new byte[_doc.GetBytesCountCached()];
        //BsonWriter.WriteDocument(data.AsSpan(), _doc, out _);
    }
}
