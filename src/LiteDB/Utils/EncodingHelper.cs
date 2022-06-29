namespace LiteDB;

public static class EncodingHelper
{
    private readonly static ArrayPool<byte> pool = ArrayPool<byte>.Shared;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EncodingResult SharedEncoding(string str, Encoding encoding)
    {
        return SharedEncoding(str, encoding, 0);
    }
    public static EncodingResult SharedEncoding(string str, Encoding encoding, int startIndex)
    {
        int byteCount = encoding.GetByteCount(str);
        byte[] bytes = pool.Rent(byteCount);
        int bytesReceived = encoding.GetBytes(str, startIndex, str.Length, bytes, 0);
        Debug.Assert(bytesReceived == byteCount);
        return new EncodingResult(bytes, bytesReceived);
    }
}

