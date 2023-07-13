namespace LiteDB;

/// <summary>
/// </summary>
[AutoInterface]
public class BsonReader : IBsonReader
{
    /// <summary>
    /// Read a document inside span buffer. 
    /// Use filds to select custom fields only (on root document only)
    /// Skip will skip document read (just update length output) - returns null
    /// Returns length document size
    /// </summary>
    public BsonDocument? ReadDocument(Span<byte> span, string[] fields, bool skip, out int length)
    {
        var doc = new BsonDocument();
        var remaining = fields.Length == 0 ? null : new HashSet<string>(fields);

        length = span.ReadVariantLength(out var varLen);

        if (skip) return null;

        var offset = varLen; // skip variable length

        while (offset < length && (remaining == null || remaining?.Count > 0))
        {
            var key = span[offset..].ReadVString(out var keyLength);

            offset += keyLength;

            var fieldSkip = remaining != null && remaining.Contains(key) == false;

            var value = this.ReadValue(span[offset..], fieldSkip, out var valueLength);

            offset += valueLength;

            if (fieldSkip == false)
            {
                doc.Add(key, value!);
            }
        }

        return doc;
    }

    public BsonArray? ReadArray(Span<byte> span, bool skip, out int length)
    {
        var array = new BsonArray();

        length = span.ReadVariantLength(out var varLen);

        if (skip) return null;

        var offset = varLen; // skip variable length

        while (offset < length)
        {
            var value = this.ReadValue(span[offset..], false, out var valueLength);

            array.Add(value!);

            offset += valueLength;
        }

        return array;
    }

    public BsonValue? ReadValue(Span<byte> span, bool skip, out int length)
    {
        var type = (BsonTypeCode)span[0];

        switch (type)
        {
            case BsonTypeCode.Double:
                length = 1 + 8;
                return skip ? null : span[1..].ReadDouble();

            case BsonTypeCode.String:
                var strLength = span[1..].ReadVariantLength(out var varSLen);
                length = 1 + varSLen + strLength;
                return skip ? null : new BsonString(span.Slice(1 + varSLen, strLength).ReadString());

            case BsonTypeCode.Document:
                var doc = this.ReadDocument(span[1..], null, skip, out var docLength);
                length = 1 + docLength;
                return doc;

            case BsonTypeCode.Array:
                var array = this.ReadArray(span[1..], skip, out var arrLength);
                length = 1 + arrLength;
                return array;

            case BsonTypeCode.Binary:
                var bytesLength = span[1..].ReadVariantLength(out var varBLen);
                length = 1 + varBLen + bytesLength;
                return skip ? null : new BsonBinary(span[(1 + varBLen)..(1 + varBLen + bytesLength)].ToArray());

            case BsonTypeCode.Guid:
                length = 1 + 16;
                return skip ? null : span[1..].ReadGuid();

            case BsonTypeCode.ObjectId:
                length = 1 + 12;
                return skip ? null : new BsonObjectId(span[1..].ReadObjectId());

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
