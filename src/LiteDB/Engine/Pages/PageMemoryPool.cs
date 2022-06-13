namespace LiteDB.Engine;

/// <summary>
/// </summary>
internal class PageMemoryPool
{
    /// <summary>
    /// Return a rent Memory[byte] for a PAGE_SIZE
    /// </summary>
    /// <returns></returns>
    public static IMemoryOwner<byte> Rent()
    {
        var owner = MemoryPool<byte>.Shared.Rent(PAGE_SIZE);

        //**owner.Memory.Span.Fill(0); precisa? não vai ser sempre algo por cima?

        return owner;
    }
}