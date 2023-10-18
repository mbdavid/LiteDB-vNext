using LiteDB.Document.Bson;

namespace LiteDB.Benchmark.Tests;

[RPlotExporter]
[MemoryDiagnoser]

public class BsonDocumentReaderTests
{
    BsonDocument document = new BsonDocument()
    {
        ["_id"] = 16,
        ["name"] = "altolfo",
        ["serial"] = "12",
        ["arr"] = new BsonArray() { 10, 11, 12, 13 },
        ["doc0"] = new BsonDocument()
        {
            ["aaa"] = 10,
            ["bbb"] = "asdfrgfdsgfsdgsdfgsfdgsdgfsd",
            ["phone"] = "1234567890",
            ["arr"] = new BsonArray() { 10, 11, 12, 13 }
        },
        ["serial1"] = "12",
        ["doc1"] = new BsonDocument()
        {
            ["aaa"] = 10,
            ["bbb"] = "asdfrgfdsgfsdgsdfgsfdgsdgfsd",
            ["phone"] = "1234567890",
            ["arr"] = new BsonArray() { 10, 11, 12, 13 }
        },
        ["serial2"] = "12",
        ["doc2"] = new BsonDocument()
        {
            ["aaa"] = 10,
            ["bbb"] = "asdfrgfdsgfsdgsdfgsfdgsdgfsd",
            ["phone"] = "1234567890",
            ["arr"] = new BsonArray() { 10, 11, 12, 13 }
        },
        ["serial3"] = "12",
        ["doc3"] = new BsonDocument()
        {
            ["aaa"] = 10,
            ["bbb"] = "asdfrgfdsgfsdgsdfgsfdgsdgfsd",
            ["phone"] = "1234567890",
            ["arr"] = new BsonArray() { 10, 11, 12, 13 }
        },
        ["serial4"] = "12",
        ["doc4"] = new BsonDocument()
        {
            ["aaa"] = 10,
            ["bbb"] = "asdfrgfdsgfsdgsdfgsfdgsdgfsd",
            ["phone"] = "1234567890",
            ["arr"] = new BsonArray() { 10, 11, 12, 13 }
        },
        ["serial5"] = "12",
        ["doc5"] = new BsonDocument()
        {
            ["aaa"] = 10,
            ["bbb"] = "asdfrgfdsgfsdgsdfgsfdgsdgfsd",
            ["phone"] = "1234567890",
            ["arr"] = new BsonArray() { 10, 11, 12, 13 }
        },
        ["serial6"] = "12",
        ["doc6"] = new BsonDocument()
        {
            ["aaa"] = 10,
            ["bbb"] = "asdfrgfdsgfsdgsdfgsfdgsdgfsd",
            ["phone"] = "1234567890",
            ["arr"] = new BsonArray() { 10, 11, 12, 13 }
        },
        ["serial7"] = "12",
        ["doc7"] = new BsonDocument()
        {
            ["aaa"] = 10,
            ["bbb"] = "asdfrgfdsgfsdgsdfgsfdgsdgfsd",
            ["phone"] = "1234567890",
            ["arr"] = new BsonArray() { 10, 11, 12, 13 }
        },
        ["serial8"] = "12",
        ["doc8"] = new BsonDocument()
        {
            ["aaa"] = 10,
            ["bbb"] = "asdfrgfdsgfsdgsdfgsfdgsdgfsd",
            ["phone"] = "1234567890",
            ["arr"] = new BsonArray() { 10, 11, 12, 13 }
        },
        ["serial9"] = "12",
        ["doc9"] = new BsonDocument()
        {
            ["aaa"] = 10,
            ["bbb"] = "asdfrgfdsgfsdgsdfgsfdgsdgfsd",
            ["phone"] = "1234567890",
            ["arr"] = new BsonArray() { 10, 11, 12, 13 }
        },
        ["phone"] = "1234567890"
    };
    //BsonDocument document = new BsonDocument()
    //{
    //    ["_id"] = 16,
    //    ["int32"] = 12,
    //    ["int64"] = 12L,
    //    ["double"] = 2.6d,
    //    ["decimal"] = new BsonDecimal(12),
    //    ["string"] = "antonio",
    //    ["int32"] = 12,
    //    ["doc"] = new BsonDocument()
    //    {
    //        ["name"] = "antonio",
    //        ["age"] = 18
    //    },
    //    ["number"] = 21,
    //    ["arr0"] = new BsonArray() {
    //            10,
    //            12L,
    //            new BsonArray() { 10, 11, 12, 13, 14, 15, 16, 17, 18},
    //            2.6d,
    //            new BsonDecimal(12),
    //            "antonio",
    //            12,
    //            new BsonDocument()
    //            {
    //                ["name"] = "antonio",
    //                ["age"] = 18
    //            },
    //            21,
    //            new BsonBinary(new byte[4] { 16, 16, 16, 16}),
    //                 },
    //    ["arr1"] = new BsonArray() {
    //            10,
    //            12L,
    //            new BsonArray() { 10, 11, 12, 13, 14, 15, 16, 17, 18},
    //            2.6d,
    //            new BsonDecimal(12),
    //            "antonio",
    //            12,
    //            new BsonDocument()
    //            {
    //                ["name"] = "antonio",
    //                ["age"] = 18
    //            },
    //            21,
    //            new BsonBinary(new byte[4] { 16, 16, 16, 16}),
    //                 },
    //    ["arr2"] = new BsonArray() {
    //            10,
    //            12L,
    //            new BsonArray() { 10, 11, 12, 13, 14, 15, 16, 17, 18},
    //            2.6d,
    //            new BsonDecimal(12),
    //            "antonio",
    //            12,
    //            new BsonDocument()
    //            {
    //                ["name"] = "antonio",
    //                ["age"] = 18
    //            },
    //            21,
    //            new BsonBinary(new byte[4] { 16, 16, 16, 16}),
    //                 },
    //    ["arr3"] = new BsonArray() {
    //            10,
    //            12L,
    //            new BsonArray() { 10, 11, 12, 13, 14, 15, 16, 17, 18},
    //            2.6d,
    //            new BsonDecimal(12),
    //            "antonio",
    //            12,
    //            new BsonDocument()
    //            {
    //                ["name"] = "antonio",
    //                ["age"] = 18
    //            },
    //            21,
    //            new BsonBinary(new byte[4] { 16, 16, 16, 16}),
    //                 },
    //    ["ficha"] = new BsonDocument()
    //    {
    //        ["name"] = "Roberto",
    //        ["age"] = 26,
    //        ["CPF"] = 0123456789,
    //        ["CEP"] = 9876543210,
    //        ["filiacao"] = "Antonia Rocha"
    //    },
    //    ["binary"] = new BsonBinary(new byte[4] { 16, 16, 16, 16 }),
    //    ["serial"] = 32
    //};
    //BsonDocument document = new BsonDocument()
    //{
    //    ["_id"] = 16,
    //    ["name"] = "antonio",
    //    ["age"] = 12,
    //    ["arr"] = new BsonArray() {
    //        10,
    //        12L,
    //        new BsonArray() { 10, 11, 12, 13, 14, 15, 16, 17, 18},
    //        2.6d,
    //        new BsonDecimal(12),
    //        "antonio",
    //        12,
    //        new BsonDocument()
    //        {
    //            ["name"] = "antonio",
    //            ["age"] = 18
    //        },
    //        21,
    //        new BsonBinary(new byte[4] { 16, 16, 16, 16}),
    //             },
    //    ["page"] = 12
    //};

    [Benchmark]
    public void BsonDocumentWriter_SingleSpan()
    {
        #region Arrange
        var documentSize = document.GetBytesCountCached() + 4;
        var span = new Span<byte>(new byte[documentSize]);
        var bw = new BsonWriter();
        bw.WriteDocument(span, document, out _);
        var bdr = new BsonDocumentReader();
        #endregion

        bdr.ReadDocument(span, new string[] { }, false);
    }

    [Benchmark]
    public void BsonWriter_SingleSpan()
    {
        #region Arrange
        var documentSize = document.GetBytesCountCached() + 4;
        var span = new Span<byte>(new byte[documentSize]);
        var bw = new BsonWriter();
        bw.WriteDocument(span, document, out _);
        var br = new BsonReader();
        #endregion

        br.ReadDocument(span, new string[] { }, false, out _);
    }

    /*[Benchmark]
    public void BsonDocumentWriter_SegmentedSpans()
    {

    }

    [Benchmark]
    public void BsonWriter_SegmentedSpans()
    {

    }*/
}

