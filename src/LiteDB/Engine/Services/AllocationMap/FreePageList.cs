namespace LiteDB.Engine;

/// <summary>
/// Implement a internal queue of free pages from a collection/bucket (data1,2,3,index)
/// </summary>
internal class FreePageList
{
    private readonly Queue<uint> _pages = new ();

    /// <summary>
    /// Get/Set to indicate this list was not fully loaded because reach limit
    /// </summary>
    public bool HasMore { get; set; } = false;

    public FreePageList()
    {
    }

    public int Count => _pages.Count;

    /// <summary>
    /// Insert PageID at begin of queue. Used when need insert a new pageID
    /// </summary>
    public void Insert(uint pageID)
    {
        //TODO: implementar no inicio da fila
        _pages.Enqueue(pageID);
    }

    /// <summary>
    /// Add a new PageID to end of queue
    /// </summary>
    public void Enqueue(uint pageID)
    {
        _pages.Enqueue(pageID);
    }

    /// <summary>
    /// Remove first PageID from queue and move to next
    /// </summary>
    public uint Dequeue()
    {
        return _pages.Dequeue();
    }
}