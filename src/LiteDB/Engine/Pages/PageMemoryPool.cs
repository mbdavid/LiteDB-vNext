namespace LiteDB.Engine;

/// <summary>
/// </summary>
internal class PageMemoryPool
{
    private ConcurrentQueue<Memory<byte>> _free = new ();

    /// <summary>
    /// Return a rent Memory[byte] for a PAGE_SIZE
    /// </summary>
    public static IMemoryOwner<byte> Rent()
    {
        var owner = MemoryPool<byte>.Shared.Rent(PAGE_SIZE);

        //**owner.Memory.Span.Fill(0); precisa? não vai ser sempre algo por cima?

        return owner;
    }

    public class PageMemoryOwner : IMemoryOwner<byte>
    {
        public Memory<byte> Memory { get; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

public struct PageMemory
{
    public int UniqueID;
    public long Position;
    public int ShareCounter;
    public long Timestamp;

    public Memory<byte> Buffer { get; }
}