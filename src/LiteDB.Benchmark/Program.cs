// Length Variante
// -----------------


//var s0 = Convert.ToString(l, 2);
//var s2 = Convert.ToString(b1, 2);
//var s3 = Convert.ToString(b3, 2);
//
//Console.WriteLine(s0.PadLeft(8 * sizeof(int), '0'));
//Console.WriteLine(s2.PadLeft(8 * sizeof(int), '0'));
//Console.WriteLine(s3.PadLeft(8 * sizeof(int), '0'));
//
//return;

var buffer = new byte[4];

// testando os valores na escrita e leitura
for(var i = 0; i < 20_000_000; i++)
{
    var span = buffer.AsSpan();

    span.Fill(0);

    span.WriteVariantLength(i, out int lengthWrite);

    var read = span.ReadVariantLength(out var lengthRead);

    ENSURE(i == read);
    ENSURE(lengthRead == lengthWrite);
}




// Run<BsonValueCompareTests>();

//BenchmarkRunner.Run<BsonWriterTests>();

/*
var d = new BsonDocument { ["a"] = new BsonArray { 1, true } };

var bytes = new byte[d.GetBytesCount()];

BsonWriter.WriteDocument(bytes.AsSpan(), d, out _);


var nd = BsonReader.ReadDocument(bytes.AsSpan(), out _);


var eq = d == nd;

Console.WriteLine(eq);
*/