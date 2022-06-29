namespace LiteDB;

public readonly struct EncodingResult : IDisposable, IEquatable<EncodingResult>
{
    public readonly byte[] Buffers;

    public readonly int Count;

    internal EncodingResult(byte[] buffers, int count)
    {
        Buffers = buffers;
        Count = count;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(Buffers);
    }
    public override bool Equals(object obj)
    {
        if (obj is EncodingResult result)
        {
            return Equals(result);
        }
        return false;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            var h = 31 * 7 + Count;
            h = 31 * h + Buffers?.GetHashCode() ?? 0;
            return h;
        }
    }

    public bool Equals(EncodingResult other)
    {
        return other.Buffers == Buffers &&
            other.Count == Count;
    }
    public override string ToString()
    {
        return $"{{Count: {Count}}}";
    }

}