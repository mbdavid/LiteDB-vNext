namespace LiteDB;

internal static class SpanExtensions
{
    #region Read Extensions

    public static Int16 ReadInt16(this Span<byte> span)
    {
        return BinaryPrimitives.ReadInt16LittleEndian(span);
    }

    public static UInt16 ReadUInt16(this Span<byte> span)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(span);
    }

    public static Int32 ReadInt32(this Span<byte> span)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(span);
    }

    public static UInt32 ReadUInt32(this Span<byte> span)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(span);
    }

    public static Int64 ReadInt64(this Span<byte> span)
    {
        return BinaryPrimitives.ReadInt64LittleEndian(span);
    }

    public static double ReadDouble(this Span<byte> span)
    {
        return BitConverter.ToDouble(span);
    }

    public static Decimal ReadDecimal(this Span<byte> span)
    {
        var a = span.ReadInt32();
        var b = span[4..].ReadInt32();
        var c = span[8..].ReadInt32();
        var d = span[12..].ReadInt32();
        return new Decimal(new int[] { a, b, c, d });
    }

    public static ObjectId ReadObjectId(this Span<byte> span)
    {
        return new ObjectId(span);
    }

    public static Guid ReadGuid(this Span<byte> span)
    {
        return new Guid(span);
    }

    public static DateTime ReadDateTime(this Span<byte> span)
    {
        var ticks = span.ReadInt64();

        if (ticks == 0) return DateTime.MinValue;
        if (ticks == 3155378975999999999) return DateTime.MaxValue;

        return new DateTime(ticks, DateTimeKind.Utc);
    }

    public static PageAddress ReadPageAddress(this Span<byte> span)
    {
        return new PageAddress(span.ReadUInt32(), span[4]);
    }

    public static string ReadString(this Span<byte> span)
    {
        return Encoding.UTF8.GetString(span);
    }

    /// <summary>
    /// Read any BsonValue. Use 1 byte for data type, 1 byte for length (optional), 0-255 bytes to value. 
    /// For document or array, use BufferReader
    /// </summary>
/*
    public static BsonValue ReadIndexKey(this Span<byte> span, int offset)
    {
        ExtendedLengthHelper.ReadLength(buffer[offset++], buffer[offset], out var type, out var len);

        switch (type)
        {
            case BsonType.Null: return BsonValue.Null;

            case BsonType.Int32: return buffer.ReadInt32(offset);
            case BsonType.Int64: return buffer.ReadInt64(offset);
            case BsonType.Double: return buffer.ReadDouble(offset);
            case BsonType.Decimal: return buffer.ReadDecimal(offset);

            case BsonType.String:
                offset++; // for byte length
                return buffer.ReadString(offset, len);

            case BsonType.Document:
                using (var r = new BufferReader(buffer))
                {
                    r.Skip(offset); // skip first byte for value.Type
                    return r.ReadDocument();
                }
            case BsonType.Array:
                using (var r = new BufferReader(buffer))
                {
                    r.Skip(offset); // skip first byte for value.Type
                    return r.ReadArray();
                }

            case BsonType.Binary:
                offset++; // for byte length
                return buffer.ReadBytes(offset, len);
            case BsonType.ObjectId: return buffer.ReadObjectId(offset);
            case BsonType.Guid: return buffer.ReadGuid(offset);

            case BsonType.Boolean: return buffer[offset] != 0;
            case BsonType.DateTime: return buffer.ReadDateTime(offset);

            case BsonType.MinValue: return BsonValue.MinValue;
            case BsonType.MaxValue: return BsonValue.MaxValue;

            default: throw new NotImplementedException();
        }
    }
*/
    #endregion

    #region Write Extensions

    public static void WriteInt16(this Span<byte> span, Int16 value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(span, value);
    }

    public static void WriteUInt16(this Span<byte> span, UInt16 value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(span, value);
    }

    public static void WriteInt32(this Span<byte> span, Int32 value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
    }

    public static void WriteUInt32(this Span<byte> span, UInt32 value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(span, value);
    }

    public static void WriteInt64(this Span<byte> span, Int64 value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span, value);
    }

    public static void WriteDouble(this Span<byte> span, Double value)
    {
        MemoryMarshal.Write(span, ref value);
    }

    public static void WriteDecimal(this Span<byte> buffer, Decimal value)
    {
        var bits = Decimal.GetBits(value);
        buffer.WriteInt32(bits[0]);
        buffer[4..].WriteInt32(bits[1]);
        buffer[8..].WriteInt32(bits[2]);
        buffer[12..].WriteInt32(bits[3]);
    }

    public static void WriteDateTime(this Span<byte> buffer, DateTime value)
    {
        buffer.WriteInt64(value.ToUniversalTime().Ticks);
    }

    public static void WritePageAddress(this Span<byte> span, PageAddress value)
    {
        span.WriteUInt32(value.PageID);
        span[4] = value.Index;
    }

    public static void WriteGuid(this Span<byte> span, Guid value)
    {
        if (value.TryWriteBytes(span) == false) throw new ArgumentException("Span too small for Guid");
    }

    public static void WriteObjectId(this Span<byte> span, ObjectId value)
    {
        value.TryWriteBytes(span);
    }

    public static void WriteBytes(this Span<byte> span, byte[] value)
    {
        value.CopyTo(span);
    }

    public static void WriteString(this Span<byte> span, string value)
    {
        Encoding.UTF8.GetBytes(value.AsSpan(), span);
    }

    /// <summary>
    /// Wrtie any BsonValue. Use 1 byte for data type, 1 byte for length (optional), 0-255 bytes to value. 
    /// For document or array, use BufferWriter
    /// </summary>
    /*
    public static void WriteIndexKey(this Span<byte> buffer, BsonValue value, int offset)
    {
        DEBUG(IndexNode.GetKeyLength(value, true) <= MAX_INDEX_KEY_LENGTH, $"index key must have less than {MAX_INDEX_KEY_LENGTH} bytes");

        if (value.IsString)
        {
            var str = value.AsString;
            var strLength = (ushort)Encoding.UTF8.GetByteCount(str);

            ExtendedLengthHelper.WriteLength(BsonType.String, strLength, out var typeByte, out var lengthByte);

            buffer[offset++] = typeByte;
            buffer[offset++] = lengthByte;
            buffer.Write(str, offset);
        }
        else if(value.IsBinary)
        {
            var arr = value.AsBinary;

            ExtendedLengthHelper.WriteLength(BsonType.Binary, (ushort)arr.Length, out var typeByte, out var lengthByte);

            buffer[offset++] = typeByte;
            buffer[offset++] = lengthByte;
            buffer.Write(arr, offset);
        }
        else
        {
            buffer[offset++] = (byte)value.Type;

            switch (value.Type)
            {
                case BsonType.Null:
                case BsonType.MinValue:
                case BsonType.MaxValue:
                    break;

                case BsonType.Int32: buffer.Write(value.AsInt32, offset); break;
                case BsonType.Int64: buffer.Write(value.AsInt64, offset); break;
                case BsonType.Double: buffer.Write(value.AsDouble, offset); break;
                case BsonType.Decimal: buffer.Write(value.AsDecimal, offset); break;

                case BsonType.Document:
                    using (var w = new BufferWriter(buffer))
                    {
                        w.Skip(offset); // skip offset from buffer
                        w.WriteDocument(value.AsDocument, true);
                    }
                    break;
                case BsonType.Array:
                    using (var w = new BufferWriter(buffer))
                    {
                        w.Skip(offset); // skip offset from buffer
                        w.WriteArray(value.AsArray, true);
                    }
                    break;

                case BsonType.ObjectId: buffer.Write(value.AsObjectId, offset); break;
                case BsonType.Guid: buffer.Write(value.AsGuid, offset); break;

                case BsonType.Boolean: buffer[offset] = (value.AsBoolean) ? (byte)1 : (byte)0; break;
                case BsonType.DateTime: buffer.Write(value.AsDateTime, offset); break;

                default: throw new NotImplementedException();
            }
        }
    }
    */

    #endregion
}
