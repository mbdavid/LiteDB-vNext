namespace LiteDB.Engine;

/// <summary>
/// </summary>
public class BsonReader
{
    public BsonReader()
    {
    }

    /// <summary>
    /// Read a document inside span buffer. 
    /// Use filds to select custom fields only (on root document only)
    /// Skip will skip document read (just update length output)
    /// Returns length document size
    /// </summary>
    public static BsonDocument ReadDocument(Span<byte> span, HashSet<string> fields, bool skip, out int length)
    {
        var doc = new BsonDocument();
        var remaining = fields == null || fields.Count == 0 ? null : fields;

        length = span.ReadInt32();

        if (skip) return null;

        var offset = 4; // skip ReadInt32() for length

        while (offset < length && (remaining == null || remaining?.Count > 0))
        {
            var key = span[offset..].ReadCString(out var keyLength);

            offset += keyLength;

            var fieldSkip = remaining != null && remaining.Contains(key) == false;

            var value = ReadValue(span[offset..], fieldSkip, out var valueLength);

            offset += valueLength;

            if (fieldSkip == false)
            {
                doc.Add(key, value);
            }
        }

        return doc;
    }

    public static BsonArray ReadArray(Span<byte> span, bool skip, out int length)
    {
        var array = new BsonArray();

        length = span.ReadInt32();

        if (skip) return null;

        var offset = 4; // skip ReadInt32() for length

        while (offset < length)
        {
            var value = ReadValue(span[offset..], false, out var valueLength);

            array.Add(value);

            offset += valueLength;
        }

        return array;
    }

    public static BsonValue ReadValue(Span<byte> span, bool skip, out int length)
    {
        var type = (BsonTypeCode)span[0];

        switch (type)
        {
            case BsonTypeCode.Double:
                length = 1 + 8;
                return skip ? null : span[1..].ReadDouble();

            case BsonTypeCode.String:
                var strLength = span.ReadInt32();
                length = 1 + 4 + strLength;
                return skip ? null : span[5..(5 + strLength)].ReadString();

            case BsonTypeCode.Document:
                var doc = ReadDocument(span[1..], null, skip, out var docLength);
                length = 1 + docLength;
                return doc;

            case BsonTypeCode.Array:
                var array = ReadArray(span[1..], skip, out var arrLength);
                length = 1 + arrLength;
                return array;

            case BsonTypeCode.Binary:
                var bytesLength = span[1..].ReadInt32();
                length = 1 + 4 + bytesLength;
                return skip ? null : span[5..(5 + bytesLength)].ToArray();

            case BsonTypeCode.Guid:
                length = 1 + 16;
                return skip ? null : span[1..].ReadGuid();

            case BsonTypeCode.ObjectId:
                length = 1 + 12;
                return skip ? null : span[1..].ReadObjectId();

            case BsonTypeCode.True:
                length = 1;
                return skip ? null : BsonBoolean.True;

            case BsonTypeCode.False:
                length = 1;
                return skip ? null : BsonBoolean.False;

            case BsonTypeCode.DateTime:
                length = 1 + 8;
                return skip ? null : span[1..].ReadDateTime();

            case BsonTypeCode.Null:
                length = 1;
                return skip ? null : BsonNull.Null;

            case BsonTypeCode.Int32:
                length = 1 + 4;
                return skip ? null : span[1..].ReadInt32();

            case BsonTypeCode.Int64:
                length = 1 + 8;
                return skip ? null : span[1..].ReadInt64();

            case BsonTypeCode.Decimal:
                length = 1 + 16;
                return skip ? null : span[1..].ReadDecimal();

            case BsonTypeCode.MinValue:
                length = 1;
                return skip ? null : BsonMinValue.MinValue;

            case BsonTypeCode.MaxValue:
                length = 1;
                return skip ? null : BsonMaxValue.MaxValue;
        }

        throw new ArgumentException();
    }
}
