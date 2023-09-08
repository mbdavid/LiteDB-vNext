namespace LiteDB;

internal unsafe static class MarshalEx
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillZero(byte* ptr, int length)
    {
        var span = new Span<byte>(ptr, length);

        span.Fill(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Copy(byte* sourcePtr, byte* destPtr, int length)
    {
        var source = new Span<byte>(sourcePtr, length);
        var dest = new Span<byte>(destPtr, length);

        source.CopyTo(dest);
    }
}