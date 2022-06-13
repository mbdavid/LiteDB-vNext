namespace LiteDB;

internal static class SpanExtensions
{
    #region Read Extensions

    public static bool ReadBool(this Span<byte> span, int offset)
    {
        return span[offset] != 0;
    }

    public static byte ReadByte(this Span<byte> span, int offset)
    {
        return span[offset];
    }

    public static Int16 ReadInt16(this Span<byte> span, int offset)
    {
        return BinaryPrimitives.ReadInt16LittleEndian(span[offset..]);
    }

    public static UInt16 ReadUInt16(this Span<byte> span, int offset)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(span[offset..]);
    }

    public static Int32 ReadInt32(this Span<byte> span, int offset)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(span[offset..]);
    }

    public static UInt32 ReadUInt32(this Span<byte> span, int offset)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(span[offset..]);
    }

    public static Int64 ReadInt64(this Span<byte> span, int offset)
    {
        return BinaryPrimitives.ReadInt64LittleEndian(span[offset..]);
    }

    public static double ReadDouble(this Span<byte> span, int offset)
    {
        return BitConverter.ToDouble(span[offset..]);
    }

    public static Decimal ReadDecimal(this Span<byte> span, int offset)
    {
        var a = span.ReadInt32(offset);
        var b = span.ReadInt32(offset + 4);
        var c = span.ReadInt32(offset + 8);
        var d = span.ReadInt32(offset + 12);
        return new Decimal(new int[] { a, b, c, d });
    }

    public static ObjectId ReadObjectId(this Span<byte> span, int offset)
    {
        var timestamp = span.ReadInt32(offset);
        var machine = span.ReadInt32(offset + 4);
        var pid = span.ReadInt16(offset + 8);
        var increment = span.ReadInt32(offset + 10);

        return new ObjectId(timestamp, machine, pid, increment);
    }

    public static Guid ReadGuid(this Span<byte> span, int offset)
    {
        return new Guid(span[offset..]);
    }

    public static byte[] ReadBytes(this Span<byte> span, int offset, int count)
    {
        return span[offset..count].ToArray();
    }

    public static DateTime ReadDateTime(this Span<byte> span, int offset)
    {
        var ticks = span.ReadInt64(offset);

        if (ticks == 0) return DateTime.MinValue;
        if (ticks == 3155378975999999999) return DateTime.MaxValue;

        return new DateTime(ticks, DateTimeKind.Utc);
    }

    public static PageAddress ReadPageAddress(this Span<byte> span, int offset)
    {
        return new PageAddress(span.ReadUInt32(offset), span[offset + 4]);
    }

    public static string ReadString(this Span<byte> span, int offset, int count)
    {
        return Encoding.UTF8.GetString(span[offset..count]);
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

    public static void Write(this Span<byte> span, bool value, int offset)
    {
        span[offset] = value ? (byte)1 : (byte)0;
    }

    public static void Write(this Span<byte> span, byte value, int offset)
    {
        span[offset] = value;
    }

    public static void Write(this Span<byte> span, Int16 value, int offset)
    {
        BinaryPrimitives.WriteInt16LittleEndian(span[offset..], value);
    }

    public static void Write(this Span<byte> span, UInt16 value, int offset)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], value);
    }

    public static void Write(this Span<byte> span, Int32 value, int offset)
    {
        BinaryPrimitives.WriteInt32LittleEndian(span[offset..], value);
    }

    public static void Write(this Span<byte> span, UInt32 value, int offset)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], value);
    }

    public static void Write(this Span<byte> span, Int64 value, int offset)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span[offset..], value);
    }

    public static void Write(this Span<byte> span, Double value, int offset)
    {
        MemoryMarshal.Write(span[offset..], ref value);
    }

    public static void Write(this Span<byte> buffer, Decimal value, int offset)
    {
        var bits = Decimal.GetBits(value);
        buffer.Write(bits[0], offset);
        buffer.Write(bits[1], offset + 4);
        buffer.Write(bits[2], offset + 8);
        buffer.Write(bits[3], offset + 12);
    }

    public static void Write(this Span<byte> buffer, DateTime value, int offset)
    {
        buffer.Write(value.ToUniversalTime().Ticks, offset);
    }

    public static void Write(this Span<byte> span, PageAddress value, int offset)
    {
        span.Write(value.PageID, offset);
        span.Write(value.Index, offset + 4);
    }

    public static void Write(this Span<byte> span, Guid value, int offset)
    {
        value.ToByteArray().CopyTo(span[offset..]);
    }

    public static void Write(this Span<byte> span, ObjectId value, int offset)
    {
        span.Write(value.Timestamp, offset);
        span.Write(value.Machine, offset + 4);
        span.Write(value.Pid, offset + 8);
        span.Write(value.Increment, offset + 10);
    }

    public static void Write(this Span<byte> span, byte[] value, int offset)
    {
        value.CopyTo(span[offset..]);
    }

    public static void Write(this Span<byte> span, string value, int offset)
    {
        Encoding.UTF8.GetBytes(value.AsSpan(), span[offset..]);
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
