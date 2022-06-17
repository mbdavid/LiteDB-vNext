namespace LiteDB.Engine;

/// <summary>
/// </summary>
public class BsonWriter
{
    public BsonWriter()
    {
    }

    public static void WriteDocument(Span<byte> span, BsonDocument document, out int length)
    {
        var offset = 4; // skip WriteInt32(length)

        foreach (var el in document.GetElements())
        {
            // write key as CString (\0)
            span[offset..].WriteCString(el.Key, out var keyLength);

            offset += keyLength;

            WriteValue(span[offset..], el.Value, out var valueOffset);

            offset += valueOffset;
        }

        length = offset;

        span.WriteInt32(length);
    }

    public static void WriteArray(Span<byte> span, BsonArray array, out int length)
    {
        var offset = 4; // skip WriteInt32(length)

        for (var i = 0; i < array.Count; i++)
        {
            WriteValue(span[offset..], array[i] ?? BsonValue.Null, out var valueLength);

            offset += valueLength;
        }

        length = offset;

        span.WriteInt32(length);
    }

    /// <summary>
    /// Write DataTypeCode + Value. Returns length (including dataType byte code)
    /// </summary>
    private static void WriteValue(Span<byte> span, BsonValue value, out int length)
    {
        switch (value.Type)
        {
            case BsonType.Double:
                span[0] = (byte)BsonTypeCode.Double;
                span[1..].WriteDouble(value.AsDouble);
                length = 1 + 8;
                break;

            case BsonType.String:
                span[0] = (byte)BsonTypeCode.String;
                var strLength = Encoding.UTF8.GetByteCount(value.AsString);
                span.WriteInt32(strLength);
                span[5..].WriteString(value.AsString);
                length = 1 + strLength + 4;
                break;

            case BsonType.Document:
                span[0] = (byte)BsonTypeCode.Document;
                WriteDocument(span, value.AsDocument, out var docLength);
                length = 1 + docLength;
                break;

            case BsonType.Array:
                span[0] = (byte)BsonTypeCode.Array;
                WriteArray(span[1..], value.AsArray, out var arrayLength);
                length = 1 + arrayLength;
                break;

            case BsonType.Binary:
                span[0] = (byte)BsonTypeCode.Binary;
                var binaryLength = value.AsBinary.Length;
                span[1..].WriteInt32(binaryLength);
                span[1..binaryLength].WriteBytes(value.AsBinary);
                length = 1 + binaryLength;
                break;

            case BsonType.Guid:
                span[0] = (byte)BsonTypeCode.Guid;
                span[1..].WriteGuid(value.AsGuid);
                length = 1 + 16;
                break;

            case BsonType.ObjectId:
                span[0] = (byte)BsonTypeCode.ObjectId;
                span[1..].WriteObjectId(value.AsObjectId);
                length = 1 + 12;
                break;

            case BsonType.Boolean:
                span[0] = value.AsBoolean ? (byte)BsonTypeCode.True : (byte)BsonTypeCode.False;
                length = 1;
                break;

            case BsonType.DateTime:
                span[0] = (byte)BsonTypeCode.DateTime;
                span[1..].WriteDateTime(value.AsDateTime);
                length = 1 + 8;
                break;

            case BsonType.Null:
                span[0] = (byte)BsonTypeCode.Null;
                length = 1;
                break;

            case BsonType.Int32:
                span[0] = (byte)BsonTypeCode.Int32;
                span[1..].WriteInt32(value.AsInt32);
                length = 1 + 4;
                break;

            case BsonType.Int64:
                span[0] = (byte)BsonTypeCode.Int64;
                span[1..].WriteInt64(value.AsInt64);
                length = 1 + 8;
                break;

            case BsonType.Decimal:
                span[0] = (byte)BsonTypeCode.Decimal;
                span[1..].WriteDecimal(value.AsDecimal);
                length = 1 + 16;
                break;

            case BsonType.MinValue:
                span[0] = (byte)BsonTypeCode.MinValue;
                length = 1;
                break;

            case BsonType.MaxValue:
                span[0] = (byte)BsonTypeCode.MaxValue;
                length = 1;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(BsonValue.Type));
        }
    }
}
