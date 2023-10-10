

using LiteDB.Benchmark.Tests;

BenchmarkRunner.Run<BsonDocumentWriterTests>();

//var d = new BsonDocument();
//var tt = 0;

//for(var c = 1; c <= 250; c++)
//{
//    var indexes = new BsonDocument();

//    for(var i = 0; i < 32; i++)
//    {
//        var indexName = Guid.NewGuid()
//            .ToString("n")
//            .Substring(0, 32);

//        indexes[indexName] = new BsonDocument
//        {
//            ["h"] = new byte[5],
//            ["l"] = new byte[5],
//            ["s"] = i,
//            ["u"] = true,
//            ["e"] = new byte[60],
//        };

//        tt += 32 + 5 + 5 + 1 + 1 + 60;
//    }

//    var colName = Guid.NewGuid()
//        .ToString("n")
//        .Substring(0, 32);

//    tt += 33;

//    d[colName] = indexes;
//}

//var total = 8192 * 8;

//Console.WriteLine(d.ToString());
//Console.WriteLine("Disponivel: " + total.ToString());
//Console.WriteLine("Cru: " + tt);
//Console.WriteLine(d.GetBytesCount());
//Console.WriteLine(d.GetBytesCount() > total ? "NÃO cabe" : "Cabe");

//Console.ReadLine();