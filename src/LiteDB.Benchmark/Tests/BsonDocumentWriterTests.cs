using LiteDB.Document.Bson;

namespace LiteDB.Benchmark.Tests;

[RPlotExporter]
[MemoryDiagnoser]

public class BsonDocumentWriterTests
{
    BsonDocument document = new BsonDocument()
    {
        ["_id"] = 16,
        ["int32"] = 12,
        ["int64"] = 12L,
        ["double"] = 2.6d,
        ["decimal"] = new BsonDecimal(12),
        ["string"] = "antonio",
        ["int32"] = 12,
        ["doc"] = new BsonDocument()
        {
            ["name"] = "antonio",
            ["age"] = 18
        },
        ["number"] = 21,
        ["arr0"] = new BsonArray() {
        10,
        12L,
        new BsonArray() { 10, 11, 12, 13, 14, 15, 16, 17, 18 },
        2.6d,
        new BsonDecimal(12),
        "antonio",
        12,
        new BsonDocument()
        {
            ["name"] = "antonio",
            ["age"] = 18
        },
        21,
        new BsonBinary(new byte[4] { 16, 16, 16, 16 }),
    },
        ["arr1"] = new BsonArray() {
        10,
        12L,
        new BsonArray() { 10, 11, 12, 13, 14, 15, 16, 17, 18 },
        2.6d,
        new BsonDecimal(12),
        "antonio",
        12,
        new BsonDocument()
        {
            ["name"] = "antonio",
            ["age"] = 18
        },
        21,
        new BsonBinary(new byte[4] { 16, 16, 16, 16 }),
    },
        ["arr2"] = new BsonArray() {
        10,
        12L,
        new BsonArray() { 10, 11, 12, 13, 14, 15, 16, 17, 18 },
        2.6d,
        new BsonDecimal(12),
        "antonio",
        12,
        new BsonDocument()
        {
            ["name"] = "antonio",
            ["age"] = 18
        },
        21,
        new BsonBinary(new byte[4] { 16, 16, 16, 16 }),
    },
        ["arr3"] = new BsonArray() {
        10,
        12L,
        new BsonArray() { 10, 11, 12, 13, 14, 15, 16, 17, 18 },
        2.6d,
        new BsonDecimal(12),
        "antonio",
        12,
        new BsonDocument()
        {
            ["name"] = "antonio",
            ["age"] = 18
        },
        21,
        new BsonBinary(new byte[4] { 16, 16, 16, 16 }),
    },
        ["ficha0"] = new BsonDocument()
        {
            ["name"] = "Roberto",
            ["age"] = 26,
            ["CPF"] = 0123456789,
            ["CEP"] = 9876543210,
            ["filiacao"] = "Antonia Rocha",
            ["bio"] = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
        },
        ["ficha1"] = new BsonDocument()
        {
            ["name"] = "Roberto",
            ["age"] = 26,
            ["CPF"] = 0123456789,
            ["CEP"] = 9876543210,
            ["filiacao"] = "Antonia Rocha",
            ["bio"] = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
        },
        ["ficha2"] = new BsonDocument()
        {
            ["name"] = "Roberto",
            ["age"] = 26,
            ["CPF"] = 0123456789,
            ["CEP"] = 9876543210,
            ["filiacao"] = "Antonia Rocha",
            ["bio"] = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
        },
        ["ficha3"] = new BsonDocument()
        {
            ["name"] = "Roberto",
            ["age"] = 26,
            ["CPF"] = 0123456789,
            ["CEP"] = 9876543210,
            ["filiacao"] = "Antonia Rocha",
            ["bio"] = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
        },
        ["ficha4"] = new BsonDocument()
        {
            ["name"] = "Roberto",
            ["age"] = 26,
            ["CPF"] = 0123456789,
            ["CEP"] = 9876543210,
            ["filiacao"] = "Antonia Rocha",
            ["bio"] = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
        },
        ["ficha5"] = new BsonDocument()
        {
            ["name"] = "Roberto",
            ["age"] = 26,
            ["CPF"] = 0123456789,
            ["CEP"] = 9876543210,
            ["filiacao"] = "Antonia Rocha",
            ["bio"] = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
        },
        ["ficha6"] = new BsonDocument()
        {
            ["name"] = "Roberto",
            ["age"] = 26,
            ["CPF"] = 0123456789,
            ["CEP"] = 9876543210,
            ["filiacao"] = "Antonia Rocha",
            ["bio"] = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
        },
        ["ficha7"] = new BsonDocument()
        {
            ["name"] = "Roberto",
            ["age"] = 26,
            ["CPF"] = 0123456789,
            ["CEP"] = 9876543210,
            ["filiacao"] = "Antonia Rocha",
            ["bio"] = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
        },
        ["ficha8"] = new BsonDocument()
        {
            ["name"] = "Roberto",
            ["age"] = 26,
            ["CPF"] = 0123456789,
            ["CEP"] = 9876543210,
            ["filiacao"] = "Antonia Rocha",
            ["bio"] = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
        },
        ["ficha9"] = new BsonDocument()
        {
            ["name"] = "Roberto",
            ["age"] = 26,
            ["CPF"] = 0123456789,
            ["CEP"] = 9876543210,
            ["filiacao"] = "Antonia Rocha",
            ["bio"] = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
        },
        ["binary"] = new BsonBinary(new byte[4] { 16, 16, 16, 16 }),
        ["serial"] = 32
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
        var bdw = new BsonDocumentWriter(document);
        #endregion

        bdw.WriteSegment(span);
    }

    [Benchmark]
    public void BsonWriter_SingleSpan()
    {
        #region Arrange
        var documentSize = document.GetBytesCountCached() + 4;
        var span = new Span<byte>(new byte[documentSize]);
        var bw = new BsonWriter();
        #endregion

        bw.WriteDocument(span, document, out _);
    }

    [Benchmark]
    public void BsonDocumentWriter_SegmentedSpans()
    {
        #region Arrange
        var documentSize = document.GetBytesCountCached() + 4;
        var span = new Span<byte>(new byte[documentSize]);
        var bdw = new BsonDocumentWriter(document);
        #endregion


        bdw.WriteSegment(span[   0..1558]);
        bdw.WriteSegment(span[1558..3116]);
        bdw.WriteSegment(span[3116..4674]);
        bdw.WriteSegment(span[4674..6232]);
    }

    [Benchmark]
    public void BsonWriter_SegmentedSpans()
    {
        #region Arrange
        var documentSize = document.GetBytesCountCached() + 4;
        var span = new Span<byte>(new byte[documentSize]);
        var buffer = new byte[8192];
        var bw = new BsonWriter();
        #endregion

        bw.WriteDocument(buffer, document, out _);

        buffer[   0..1558].CopyTo(span[   0..1558]);
        buffer[1558..3116].CopyTo(span[1558..3116]);
        buffer[3116..4674].CopyTo(span[3116..4674]);
        buffer[4674..6232].CopyTo(span[4674..6232]);
    }
}

