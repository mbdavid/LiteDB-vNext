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
        return new PageAddress(span.ReadUInt32(), span[4]);
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

    public static int ReadVariantLength(this Span<byte> span, out int length)
    {
        if ((span[0] & 0b10000000) == 0) // first bit is 0
        {
            length = 1;
            return span[0];
        }
        else if ((span[0] & 0b11000000) == 128) // first bit is 1 but second is 0
        {
            length = 2;
            var value = span.ReadUInt16();
            var number = value & 0b01111111_11111111;
            return number;
        }
        else
        {
            length = 4;
            var value = span.ReadUInt32();
            var number = value & 0b00111111_11111111_11111111_11111111;
            return (int)number;
        }

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

    public static void WriteVString(this Span<byte> span, string value, out int length)
    {
        var strLength = Encoding.UTF8.GetByteCount(value);
        WriteVariantLength(span, strLength, out var varLen);

        Encoding.UTF8.GetBytes(value.AsSpan(), span[varLen..(varLen + strLength)]);

        length = varLen + strLength;
    }

    public static void WriteVariantLength(this Span<byte> span, int dataLength, out int varLen)
    {
        varLen = BsonValue.GetVariantLength(dataLength);

        if (varLen == 1)
        {
            span[0] = (byte)dataLength;
        }
        else if (varLen == 2)
        {
            var op = 0b10000000_00000000;
            var number = (ushort)(dataLength | op);
            span.WriteUInt16(number);
        }
        else
        {
            var op = 0b11000000_00000000_00000000_00000000;
            var number = (((uint)dataLength) | op);
            span.WriteUInt32(number);
        }
    }

    #endregion

    #region Check Methods

    public static bool IsFullZero(this Span<byte> span)
    {
        //TODO: pode ser otimizado?
        for(var i = 0; i < span.Length; i++)
        {
            if (span[i] != 0) return false;
        }

        return true;
    }

    #endregion
}
