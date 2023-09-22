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
            #region BasicTypes
            yield return new object[] {
                new BsonDocument()
                {
                        ["_id"] = 16,
                        ["name"] = "antonio",
                        ["age"] = 12,
                        ["_doc"] = new BsonDocument()
                        {
                            ["_id"] = 10,
                            ["name2"] = "antonio",
                            ["ic2"] = 10
                        },
                        ["page"] = 12
                }
            };
            #endregion
        }

        [Theory]
        [MemberData(nameof(Get_Documents))]
        public void WriteSegment_SegmentedSpan_ShouldNotChange(params object[] documentObj)
        {
            #region Arrange
            var document = documentObj.As<BsonDocument>();
            var documentSize = document.GetBytesCount();

            var bw = new BsonDocumentWriter(document);
            var span = new Span<byte>(new byte[documentSize]);
            #endregion

            #region Act
            for (int i = 8; bw.WriteSegment(span[(i-8)..i]); i += 8);
            #endregion

            #region Assert
            var reader = new BsonReader();
            var readDocument = reader.ReadDocument(span, new string[0], false, out var len);
            readDocument.Should().Be(document);
            #endregion
        }
    }
}
