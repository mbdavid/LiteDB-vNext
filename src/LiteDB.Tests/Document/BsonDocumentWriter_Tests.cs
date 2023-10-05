using LiteDB.Document.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Tests.Document
{

    public class BsonDocumentWriter_Tests
    {
        public static IEnumerable<object[]> Get_Documents()
        {
            //array
            yield return new object[] {
                new BsonDocument()
                {
                        ["_id"] = 16,
                        ["name"] = "antonio",
                        ["age"] = 12,
                        ["arr"] = new BsonArray() {
                            10,
                            12L,
                            new BsonArray() { 10, 11, 12, 13, 14, 15, 16, 17, 18},
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
                            new BsonBinary(new byte[4] { 16, 16, 16, 16}),
                        },
                        ["page"] = 12
                }
            };

            //array
            yield return new object[] {
                new BsonDocument()
                {
                        ["_id"] = 16,
                        ["name"] = "antonio",
                        ["age"] = 12,
                        ["arr"] = new BsonArray() {
                            10,
                            12L,
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
                            new BsonBinary(new byte[4] { 16, 16, 16, 16}),
                        },
                        ["page"] = 12
                }
            };

            //array
            yield return new object[] {
                new BsonDocument()
                {
                        ["_id"] = 16,
                        ["name"] = "antonio",
                        ["age"] = 12,
                        ["arr"] = new BsonArray() { 10, 11, 12, 13, 14, 15, 16, 17, 18},
                        ["page"] = 12
                }
            };

            yield return new object[] {
                new BsonDocument()
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
                        ["binary"] = new BsonBinary(new byte[4] { 16, 16, 16, 16}),
                        ["serial"] = 32
                }
            };

            yield return new object[] {
                new BsonDocument()
                {
                        ["_id"] = 16,
                        ["name"] = "antonio",
                        ["age"] = 12,
                        ["doc"] = new BsonDocument()
                        {
                            ["name2"] = "antonio",
                            ["ic"] = 10
                        },
                        ["page"] = 12,
                        ["serial"] = 32
                }
            };

            //subdocument inside subdocument
            yield return new object[] {
                new BsonDocument()
                {
                        ["_id"] = 16,
                        ["name"] = "antonio",
                        ["age"] = 12,
                        ["doc"] = new BsonDocument()
                        {
                            ["name2"] = "antonio",
                            ["ic"] = 10,
                            ["doc2"] = new BsonDocument()
                            {
                                ["name3"] = "antonio",
                                ["ic1"] = 10
                            }
                        }
                }
            };

            //subsequent subdocuments
            yield return new object[] {
                new BsonDocument()
                {
                        ["_id"] = 16,
                        ["name"] = "antonio",
                        ["age"] = 12,
                        ["doc"] = new BsonDocument()
                        {
                            ["name1"] = "antonio",
                            ["ic"] = 10
                        },
                        ["doc2"] = new BsonDocument()
                        {
                            ["name2"] = "antonio",
                            ["ic"] = 10
                        },
                        ["page"] = 12
                }
            };
        }

        [Theory]
        [MemberData(nameof(Get_Documents))]
        public void WriteSegment_SegmentedSpan_ShouldNotChange(params object[] documentObj)
        {
            #region Arrange
            var document = documentObj[0].As<BsonDocument>();
            var documentSize = document.GetBytesCount() + 4;

            var bw = new BsonDocumentWriter(document);
            var span = new Span<byte>(new byte[documentSize]);
            #endregion

            #region Act
            for (int i = 8; i<=documentSize; i += 8)
            {
                bw.WriteSegment(span[(i - 8)..i]);
            }
            #endregion

            #region Assert
            var reader = new BsonReader();
            var readDocument = reader.ReadDocument(span, Array.Empty<string>(), false, out var len);
            readDocument.Value.Should().Be(document);
            #endregion
        }

        [Theory]
        [MemberData(nameof(Get_Documents))]
        public void WriteSegment_FullSpan_ShouldNotChange(params object[] documentObj)
        {
            #region Arrange
            var document = documentObj[0].As<BsonDocument>();
            var documentSize = document.GetBytesCount();

            var bw = new BsonDocumentWriter(document);
            var span = new Span<byte>(new byte[documentSize]);
            #endregion

            #region Act
            bw.WriteSegment(span);
            #endregion

            #region Assert
            var reader = new BsonReader();
            var readDocument = reader.ReadDocument(span, new string[0], false, out var len);
            readDocument.Value.Should().Be(document);
            #endregion
        }
    }
}
