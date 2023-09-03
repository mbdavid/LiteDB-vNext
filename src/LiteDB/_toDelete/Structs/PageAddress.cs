namespace LiteDB.Engine;

/// <summary>
/// Represents a page address inside a page structure - index could be byte offset position OR index in a list (6 bytes)
/// * Immutable (thread safe)
/// </summary>
[DebuggerStepThrough]
[Obsolete]
internal struct PageAddress : IEquatable<PageAddress>
{
    public const int SIZE = 5;

    public static PageAddress Empty = new (int.MaxValue, byte.MaxValue);

    /// <summary>
    /// PageID (4 bytes)
    /// </summary>
    public readonly int PageID;

    /// <summary>
    /// Page Segment index inside page (1 bytes)
    /// </summary>
    public readonly byte Index;

    /// <summary>
    /// Returns true if this PageAdress is empty value
    /// </summary>
    public bool IsEmpty => this.PageID == int.MaxValue && this.Index == byte.MaxValue;

    public override bool Equals(object other) => this.Equals((PageAddress)other);

    public bool Equals(PageAddress other)
    {
        return this.PageID == other.PageID && this.Index == other.Index;
    }

    public static bool operator ==(PageAddress left, PageAddress right)
    {
        return left.PageID == right.PageID && left.Index == right.Index;
    }

    public static bool operator !=(PageAddress left, PageAddress right)
    {
        return !(left == right);
    }

    public override int GetHashCode() => HashCode.Combine(this.PageID, this.Index);

    public PageAddress(int pageID, byte index)
    {
        this.PageID = pageID;
        this.Index = index;
    }

    public override string ToString()
    {
        return IsEmpty ? "<EMPTY>" : $"{PageID:0000}:{Index:00}";
    }
}