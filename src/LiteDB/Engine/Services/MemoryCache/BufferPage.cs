namespace LiteDB.Engine;

/// <summary>
/// Implement a page size fixed factory for data pages. Must dispose to dealocate memory
/// </summary>
internal class BufferPage : IMemoryOwner<byte>
{
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

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_source);
    }
}
