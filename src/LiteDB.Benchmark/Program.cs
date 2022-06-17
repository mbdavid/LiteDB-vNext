// Length Variante
// -----------------

//TODO:Sombrio - testar

var buffer = new byte[4].AsSpan();

// testando os valores na escrita e leitura
for(var i = 0; i < 2000; i++)
{
    buffer.Fill(0);

    // implementar aqui!
    buffer.WriteVariantLength(i, out int lengthWrite);

    // implementar aqui!
    var read = buffer.ReadVariantLength(out var lengthRead);

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