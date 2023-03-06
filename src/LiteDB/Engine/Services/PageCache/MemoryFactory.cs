namespace LiteDB.Engine;

/// <summary>
/// [THREAD_SAFE]
/// </summary>
[AutoInterface(true)]
internal class MemoryFactory : IMemoryFactory, IDisposable
{
    private MemoryPool<byte> _pool = MemoryPool<byte>.Shared;

    public IMemoryOwner<byte> Rent()
    {
        return new PageMemory();
    }

    public void Dispose()
    {
        _pool.Dispose();
    }
}

public class PageMemory : IMemoryOwner<byte>
{
    private readonly byte[] _source;

    public PageMemory()
    {
        _source = ArrayPool<byte>.Shared.Rent(PAGE_SIZE);
        this.Memory = new Memory<byte>(_source);
    }

    public Memory<byte> Memory { get; }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_source);
    }
}
