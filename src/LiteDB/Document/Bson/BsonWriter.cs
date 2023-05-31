namespace LiteDB;

/// <summary>
/// </summary>
[AutoInterface]
public class BsonWriter : IBsonWriter
{
    public void WriteDocument(Span<byte> span, BsonDocument document, out int length)
    {
        var docLength = document.GetBytesCountCached();
        var varLength = BsonValue.GetVariantLengthFromValue(docLength);

        var offset = varLength; // skip VariantLength

        foreach (var el in document.GetElements())
        {
            span[offset..].WriteVString(el.Key, out var keyLength);

            offset += keyLength;

            this.WriteValue(span[offset..], el.Value, out var valueLength);

            offset += valueLength;
        }

        length = offset;

        span.WriteVariantLength(length, out _);
    }

    public void WriteArray(Span<byte> span, BsonArray array, out int length)
    {
        var arrLength = array.GetBytesCountCached();
        var varLength = BsonValue.GetVariantLengthFromValue(arrLength);

        var offset = varLength; // skip VariantLength

        for (var i = 0; i < array.Count; i++)
        {
            this.WriteValue(span[offset..], array[i] ?? BsonValue.Null, out var valueLength);

            offset += valueLength;
        }

        length = offset;

        span.WriteVariantLength(length, out _);
    }

    /// <summary>
    /// Write DataTypeCode + Value. Returns length (including dataType byte code)
    /// </summary>
    public void WriteValue(Span<byte> span, BsonValue value, out int length)
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
                span[1..].WriteVariantLength(strLength, out var varLength);
                span[(1 + varLength)..].WriteString(value.AsString);
                length = 1 + varLength + strLength;
                break;

            case BsonType.Document:
                span[0] = (byte)BsonTypeCode.Document;
                this.WriteDocument(span[1..], value.AsDocument, out var docLength);
                length = 1 + docLength;
                break;

            case BsonType.Array:
                span[0] = (byte)BsonTypeCode.Array;
                this.WriteArray(span[1..], value.AsArray, out var arrayLength);
                length = 1 + arrayLength;
                break;

            case BsonType.Binary:
                span[0] = (byte)BsonTypeCode.Binary;
                var binaryLength = value.AsBinary.Length;
                span[1..].WriteVariantLength(binaryLength, out var varLen);
                span[(1 + varLen)..].WriteBytes(value.AsBinary);
                length = 1 + varLen + binaryLength;
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
