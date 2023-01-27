namespace LiteDB.Engine;

/// <summary>
/// Implement a page size fixed factory for data pages. Must dispose to dealocate memory
/// </summary>
internal class BufferPage : IMemoryOwner<byte>
{
    public static byte[] Empty { get; } = new byte[PAGE_SIZE];

    private readonly byte[] _source;

    public Memory<byte> Memory { get; }

    public BufferPage(bool clean)
    {
        _source = ArrayPool<byte>.Shared.Rent(PAGE_SIZE);

        this.Memory = new Memory<byte>(_source, 0, PAGE_SIZE);

        if (clean)
        {
            // update all page with 0
            this.Memory.Span.Fill(0);
        }
    }

    /// <summary>
    /// Checks if page buffer contains only 0
    /// </summary>
    public bool IsEmpty() => this.Memory.Span.IsFullZero();

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_source);
    }
}
