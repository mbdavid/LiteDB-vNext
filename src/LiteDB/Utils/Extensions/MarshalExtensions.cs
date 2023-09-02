namespace LiteDB;

internal unsafe static class MarshalEx
{
    public static void StrUtf8Copy(byte* strPtr, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);

        Marshal.Copy(bytes, 0, (nint)strPtr, bytes.Length);
    }

    public static string ReadStrUtf8(byte* strPtr, int bytesCount)
    {
        return Encoding.UTF8.GetString(strPtr, bytesCount);
    }

    public static uint IncrementUInt(ref uint value)
    {
        return value++;
    }

    public static void FillZero(byte* ptr, int length)
    {
        var span = new Span<byte>(ptr, length);

        span.Fill(0);
    }

    public static void Copy(byte* sourcePtr, byte* destPtr, int length)
    {
        var source = new Span<byte>(sourcePtr, length);
        var dest = new Span<byte>(destPtr, length);

        source.CopyTo(dest);
    }
}