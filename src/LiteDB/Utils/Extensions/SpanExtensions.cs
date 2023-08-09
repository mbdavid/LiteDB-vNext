namespace LiteDB;

internal static class SpanExtensions
{
    private static readonly IBsonReader _reader = new BsonReader();
    private static readonly IBsonWriter _writer = new BsonWriter();

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
        return new Guid(span[..16]);
    }

    public static DateTime ReadDateTime(this Span<byte> span, bool utc = true)
    {
        var ticks = span.ReadInt64();

        if (ticks == 0) return DateTime.MinValue;
        if (ticks == 3155378975999999999) return DateTime.MaxValue;

        var utcDate = new DateTime(ticks, DateTimeKind.Utc);

        return utc ? utcDate : utcDate.ToLocalTime();
    }

    public static PageAddress ReadPageAddress(this Span<byte> span)
    {
        return new PageAddress(span.ReadInt32(), span[4]);
    }

    public static string ReadString(this Span<byte> span)
    {
        return Encoding.UTF8.GetString(span);
    }

    public static string ReadVString(this Span<byte> span, out int length)
    {
        var strLength = ReadVariantLength(span, out var varLen);

        length = varLen + strLength;

        return Encoding.UTF8.GetString(span[varLen..(varLen + strLength)]);
    }

    public static int ReadVariantLength(this Span<byte> span, out int varLen)
    {
        if ((span[0] & 0b10000000) == 0) // first bit is 0
        {
            varLen = 1;
            return span[0];
        }
        else if ((span[0] & 0b11000000) == 128) // first bit is 1 but second is 0
        {
            varLen = 2;
            var value = BinaryPrimitives.ReadUInt16BigEndian(span);
            var number = value & 0b01111111_11111111;
            return number;
        }
        else
        {
            varLen = 4;
            var value = BinaryPrimitives.ReadUInt32BigEndian(span);
            var number = value & 0b00111111_11111111_11111111_11111111;
            return (int)number;
        }

    }

    /// <summary>
    /// Read a BsonValue from Span using singleton instance of IBsonReader. Used for IndexKey node
    /// </summary>
    public static BsonValue ReadBsonValue(this Span<byte> span, out int length)
    {
        var result = _reader.ReadValue(span, false, out length)!; // skip = false - always returns a BsonValue

        if (result.Fail) throw result.Exception!;

        return result.Value!;
    }

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
        span.WriteInt32(value.PageID);
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

    public static void WriteVString(this Span<byte> span, string value, out int length)
    {
        var strLength = Encoding.UTF8.GetByteCount(value);
        WriteVariantLength(span, strLength, out var varLen);

        Encoding.UTF8.GetBytes(value.AsSpan(), span[varLen..(varLen + strLength)]);

        length = varLen + strLength;
    }

    /// <summary>
    /// Write dataLen using 1, 2 or 4 bytes to store length
    /// </summary>
    public static void WriteVariantLength(this Span<byte> span, int dataLength, out int varLen)
    {
        varLen = BsonValue.GetVariantLengthFromData(dataLength);

        if (varLen == 1)
        {
            span[0] = (byte)dataLength;
        }
        else if (varLen == 2)
        {
            var op = 0b10000000_00000000;
            var number = (ushort)(dataLength | op);
            BinaryPrimitives.WriteUInt16BigEndian(span, number);
        }
        else
        {
            var op = 0b11000000_00000000_00000000_00000000;
            var number = (((uint)dataLength) | op);
            BinaryPrimitives.WriteUInt32BigEndian(span, number);
        }
    }
    
    /// <summary>
    /// Write BsonValue direct into a byte[]. Used for Index Key write. Use a Singleton instance of BsonWriter
    /// </summary>
    public static void WriteBsonValue(this Span<byte> span, BsonValue value, out int length)
    {
        _writer.WriteValue(span, value, out length);
    }

    #endregion

    #region Utils

    public static Span<byte> Slice(this Span<byte> span, PageSegment segment)
    {
        return span.Slice(segment.Location, segment.Length);
    }

    public static unsafe bool IsFullZero(this Span<byte> span)
    {
        fixed (byte* bytes = span)
        {
            int len = span.Length;
            int rem = len % (sizeof(long) * 16);
            long* b = (long*)bytes;
            long* e = (long*)(bytes + len - rem);

            while (b < e)
            {
                if ((*(b) | *(b + 1) | *(b + 2) | *(b + 3) | *(b + 4) |
                    *(b + 5) | *(b + 6) | *(b + 7) | *(b + 8) |
                    *(b + 9) | *(b + 10) | *(b + 11) | *(b + 12) |
                    *(b + 13) | *(b + 14) | *(b + 15)) != 0)
                    return false;
                b += 16;
            }

            for (int i = 0; i < rem; i++)
                if (span[len - 1 - i] != 0)
                    return false;

            return true;
        }
    }

    #endregion
}
