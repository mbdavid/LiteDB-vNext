namespace LiteDB.Engine;

/// <summary>
/// </summary>
public class BsonReader
{
    public BsonReader()
    {
    }

    public static BsonDocument ReadDocument(Span<byte> span, out int length)
    {
        var doc = new BsonDocument();

        length = span.ReadInt32();

        var offset = 4; // skip ReadInt32() for length

        while (offset < length)
        {
            var key = span[offset..].ReadCString(out var keyLength);

            offset += keyLength;

            var value = ReadValue(span[offset..], out var valueLength);

            offset += valueLength;

            doc.Add(key, value);
        }

        return doc;
    }

    public static BsonArray ReadArray(Span<byte> span, out int length)
    {
        var array = new BsonArray();

        length = span.ReadInt32();

        var offset = 4; // skip ReadInt32() for length

        while (offset < length)
        {
            var value = ReadValue(span[offset..], out var valueLength);

            array.Add(value);

            offset += valueLength;
        }

        return array;
    }

    private static BsonValue ReadValue(Span<byte> span, out int length)
    {
        var type = (BsonTypeCode)span[0];

        switch (type)
        {
            case BsonTypeCode.Double:
                length = 1 + 8;
                return span[1..].ReadDouble();

            case BsonTypeCode.String:
                var strLength = span.ReadInt32();
                length = 1 + 4 + strLength;
                return span[5..(5 + strLength)].ReadString();

            case BsonTypeCode.Document:
                var doc = ReadDocument(span[1..], out var docLength);
                length = 1 + docLength;
                return doc;

            case BsonTypeCode.Array:
                var array = ReadArray(span[1..], out var arrLength);
                length = 1 + arrLength;
                return array;

            case BsonTypeCode.Binary:
                var bytesLength = span[1..].ReadInt32();
                length = 1 + 4 + bytesLength;
                return span[5..(5 + bytesLength)].ToArray();

            case BsonTypeCode.Guid:
                length = 1 + 16;
                return span[1..].ReadGuid();

            case BsonTypeCode.ObjectId:
                length = 1 + 12;
                return span[1..].ReadObjectId();

            case BsonTypeCode.True:
                length = 1;
                return BsonBoolean.True;

            case BsonTypeCode.False:
                length = 1;
                return BsonBoolean.False;

            case BsonTypeCode.DateTime:
                length = 1 + 8;
                return span[1..].ReadDateTime();

            case BsonTypeCode.Null:
                length = 1;
                return BsonNull.Null;

            case BsonTypeCode.Int32:
                length = 1 + 4;
                return span[1..].ReadInt32();

            case BsonTypeCode.Int64:
                length = 1 + 8;
                return span[1..].ReadInt64();

            case BsonTypeCode.Decimal:
                length = 1 + 16;
                return span[1..].ReadDecimal();

            case BsonTypeCode.MinValue:
                length = 1;
                return BsonMinValue.MinValue;

            case BsonTypeCode.MaxValue:
                length = 1;
                return BsonMaxValue.MaxValue;
        }

        throw new ArgumentException();
    }
}
