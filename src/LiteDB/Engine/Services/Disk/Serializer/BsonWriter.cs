namespace LiteDB.Engine;

/// <summary>
/// </summary>
public class BsonWriter
{
    public BsonWriter()
    {
    }

    public static Span<byte> WriteDocument(Span<byte> span, BsonDocument document)
    {
        span.WriteInt32(document.GetBytesCountCached());

        span = span[4..];

        foreach (var el in document.GetElements())
        {
            span = WriteElement(span, el.Key, el.Value);
        }

        span[0] = (byte)'\0';

        return span.Length == 1 ? Span<byte>.Empty : span[1..];
    }

    public static Span<byte> WriteArray(Span<byte> span, BsonArray array)
    {
        span.WriteInt32(array.GetBytesCountCached());

        span = span[4..];

        for (var i = 0; i < array.Count; i++)
        {
            span = WriteElement(span, i.ToString(), array[i] ?? BsonValue.Null);
        }

        span[0] = (byte)'\0';

        return span.Length == 1 ? Span<byte>.Empty : span[1..];
    }

    private static Span<byte> WriteElement(Span<byte> span, string key, BsonValue value)
    {
        switch (value.Type)
        {
            case BsonType.Double:
                span = WriteElementKey(span, 0x01, key);
                span.WriteDouble(value.AsDouble);
                return span[8..];

            case BsonType.String:
                span = WriteElementKey(span, 0x02, key);
                var strLength = Encoding.UTF8.GetByteCount(value.AsString);
                span.WriteInt32(strLength + 1);
                span[4..strLength].WriteString(value.AsString);
                span[4 + strLength] = (byte)'\0';
                return span[(5 + strLength)..];

            case BsonType.Document:
                span = WriteElementKey(span, 0x03, key);
                span = WriteDocument(span, value.AsDocument);
                return span;

            case BsonType.Array:
                span = WriteElementKey(span, 0x04, key);
                span = WriteArray(span, value.AsArray);
                return span;

            case BsonType.Binary:
                span = WriteElementKey(span, 0x05, key);
                span.WriteBytes(value.AsBinary);
                return span[value.AsBinary.Length..];

            case BsonType.Guid:
                span = WriteElementKey(span, 0x05, key);
                span.WriteGuid(value.AsGuid);
                return span[16..];

            case BsonType.ObjectId:
                span = WriteElementKey(span, 0x12, key);
                span.WriteObjectId(value.AsObjectId);
                return span[12..];

            case BsonType.Boolean:
                span = WriteElementKey(span, 0x12, key);
                span[0] = value.AsBoolean ? (byte)1 : (byte)0;
                return span[1..];

            case BsonType.DateTime:
                span = WriteElementKey(span, 0x12, key);
                span.WriteDateTime(value.AsDateTime);
                return span[8..];

            case BsonType.Null:
                return WriteElementKey(span, 0x0A, key);

            case BsonType.Int32:
                span = WriteElementKey(span, 0x10, key);
                span.WriteInt32(value.AsInt32);
                return span[4..];

            case BsonType.Int64:
                span = WriteElementKey(span, 0x12, key);
                span.WriteInt64(value.AsInt64);
                return span[8..];

            case BsonType.Decimal:
                span = WriteElementKey(span, 0x13, key);
                span.WriteDecimal(value.AsDecimal);
                return span[16..];

            case BsonType.MinValue:
                return WriteElementKey(span, 0xFF, key);

            case BsonType.MaxValue:
                return WriteElementKey(span, 0x7F, key);
        }

        throw new ArgumentException();
    }

    /// <summary>
    /// Write on buffer a dataType and key element. Returns new span in sequence
    /// </summary>
    private static Span<byte> WriteElementKey(Span<byte> span, byte dataType, string key)
    {
        span[0] = dataType;
        span[1..].WriteString(key);

        var keyLength = Encoding.UTF8.GetByteCount(key);

        span[keyLength + 1] = (byte)'\0';

        return span[(1 + keyLength + 1)..]; // dataType + string + \0
    }
}
