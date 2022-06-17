
// Run<BsonValueCompareTests>();

//BenchmarkRunner.Run<BsonWriterTests>();

var d = new BsonDocument { ["a"] = new BsonArray { 1, true } };

var bytes = new byte[d.GetBytesCount()];

BsonWriter.WriteDocument(bytes.AsSpan(), d, out _);


var nd = BsonReader.ReadDocument(bytes.AsSpan(), out _);


var eq = d == nd;

Console.WriteLine(eq);