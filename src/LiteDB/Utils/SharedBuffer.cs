namespace LiteDB;

/// <summary>
/// A shared byte array to rent and return on dispose
/// </summary>
internal readonly struct SharedBuffer : IDisposable
{
    private readonly byte[] _array;
    private readonly int _length;

    private SharedBuffer(byte[] array, int length)
    {
        _array = array;
        _length = length;
    }

    public readonly Span<byte> AsSpan() => _array.AsSpan(0, _length);

    public readonly Span<byte> AsSpan(int start) => _array.AsSpan(start);

    public readonly Span<byte> AsSpan(int start, int length) => _array.AsSpan(start, length);

    public static SharedBuffer Rent(int length)
    {
        ENSURE(length < int.MaxValue, new { length });

        var array = ArrayPool<byte>.Shared.Rent(length);

        return new (array, length);
    }

    public void CopyFrom(Span<byte> source)
    {
        
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_array);
    }

    public override string ToString()
    {
        return Dump.Object(new { _length });
    }
}