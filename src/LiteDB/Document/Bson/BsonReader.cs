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
    public BsonReadResult ReadDocument(Span<byte> span, string[] fields, bool skip, out int length)
    {
        var doc = new BsonDocument();
        var remaining = fields.Length == 0 ? null : new HashSet<string>(fields);

        length = 0;

        try
        {
            length = span.ReadVariantLength(out var varLen);

            if (skip)
            {
                return BsonReadResult.Empty;
            }

            var offset = varLen; // skip variable length

            while (offset < length && (remaining == null || remaining?.Count > 0))
            {
                var key = span[offset..].ReadVString(out var keyLength);

                offset += keyLength;

                var fieldSkip = remaining != null && remaining.Contains(key) == false;

                var valueResult = this.ReadValue(span[offset..], fieldSkip, out var valueLength);

                if (valueResult.Fail)
                {
                    return new(doc, valueResult.Exception!);
                }

                offset += valueLength;

                if (fieldSkip == false)
                {
                    doc.Add(key, valueResult.Value!);
                }
            }

            return doc;
        }
        catch (Exception ex)
        {
            return new(doc, ex);
        }
    }

    public BsonReadResult ReadArray(Span<byte> span, bool skip, out int length)
    {
        var array = new BsonArray();

        length = 0;

        try
        {
            length = span.ReadVariantLength(out var varLen);

            if (skip) return BsonReadResult.Empty;

            var offset = varLen; // skip variable length

            while (offset < length)
            {
                var valueResult = this.ReadValue(span[offset..], false, out var valueLength);

                if (valueResult.Ok)
                {
                    array.Add(valueResult.Value!);
                }
                else
                {
                    return new(array, valueResult.Exception!);
                }

                offset += valueLength;
            }

            return array;

        }
        catch (Exception ex)
        {
            return new(array, ex);
        }
    }

    public BsonReadResult ReadValue(Span<byte> span, bool skip, out int length)
    {
        var type = (BsonTypeCode)span[0];

        length = 0;

        try
        {
            switch (type)
            {
                case BsonTypeCode.Double:
                    length = 1 + 8;
                    return skip ? BsonReadResult.Empty : new BsonDouble(span[1..].ReadDouble());

                case BsonTypeCode.String:
                    var strLength = span[1..].ReadVariantLength(out var varSLen);
                    length = 1 + varSLen + strLength;
                    return skip ? BsonReadResult.Empty : new BsonString(span.Slice(1 + varSLen, strLength).ReadString());

                case BsonTypeCode.Document:
                    var doc = this.ReadDocument(span[1..], Array.Empty<string>(), skip, out var docLength);
                    length = 1 + docLength;
                    return doc;

                case BsonTypeCode.Array:
                    var array = this.ReadArray(span[1..], skip, out var arrLength);
                    length = 1 + arrLength;
                    return array;

                case BsonTypeCode.Binary:
                    var bytesLength = span[1..].ReadVariantLength(out var varBLen);
                    length = 1 + varBLen + bytesLength;
                    return skip ? BsonReadResult.Empty : new BsonBinary(span[(1 + varBLen)..(1 + varBLen + bytesLength)].ToArray());

                case BsonTypeCode.Guid:
                    length = 1 + 16;
                    return skip ? BsonReadResult.Empty : new BsonGuid(span[1..].ReadGuid());

                case BsonTypeCode.ObjectId:
                    length = 1 + 12;
                    return skip ? BsonReadResult.Empty : new BsonObjectId(span[1..].ReadObjectId());

                case BsonTypeCode.True:
                    length = 1;
                    return skip ? BsonReadResult.Empty : BsonBoolean.True;

                case BsonTypeCode.False:
                    length = 1;
                    return skip ? BsonReadResult.Empty : BsonBoolean.False;

                case BsonTypeCode.DateTime:
                    length = 1 + 8;
                    return skip ? BsonReadResult.Empty : new BsonDateTime(span[1..].ReadDateTime());

                case BsonTypeCode.Null:
                    length = 1;
                    return skip ? BsonReadResult.Empty : BsonNull.Null;

                case BsonTypeCode.Int32:
                    length = 1 + 4;
                    return skip ? BsonReadResult.Empty : new BsonInt32(span[1..].ReadInt32());

                case BsonTypeCode.Int64:
                    length = 1 + 8;
                    return skip ? BsonReadResult.Empty : new BsonInt64(span[1..].ReadInt64());

                case BsonTypeCode.Decimal:
                    length = 1 + 16;
                    return skip ? BsonReadResult.Empty : new BsonDecimal(span[1..].ReadDecimal());

                case BsonTypeCode.MinValue:
                    length = 1;
                    return skip ? BsonReadResult.Empty : BsonMinValue.MinValue;

                case BsonTypeCode.MaxValue:
                    length = 1;
                    return skip ? BsonReadResult.Empty : BsonMaxValue.MaxValue;
            }

            throw new ArgumentException();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
