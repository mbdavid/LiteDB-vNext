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
}