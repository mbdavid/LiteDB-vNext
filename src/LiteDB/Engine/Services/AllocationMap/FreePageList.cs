namespace LiteDB.Engine;

/// <summary>
/// Implement a internal queue of free pages from a collection/bucket (data1,2,3,index)
/// </summary>
internal class FreePageList
{
    private readonly Queue<int> _left = new();
    private readonly Queue<int> _right = new();

    /// <summary>
    /// Get/Set to indicate this list was not fully loaded because reach limit
    /// </summary>
    public bool HasMore { get; set; } = false;

    public FreePageList()
    {
    }

    public int Count => _left.Count + _right.Count;

    /// <summary>
    /// Insert PageID at begin of queue. Used when need insert a new pageID
    /// </summary>
    public void Insert(int pageID)
    {
        _left.Enqueue(pageID);
    }

    /// <summary>
    /// Add a new PageID to end of queue
    /// </summary>
    public void Enqueue(int pageID)
    {
        _right.Enqueue(pageID);
    }

    /// <summary>
    /// Return dequeue elements from  
    /// </summary>
    public int Dequeue()
    {
        if (_left.Count > 0)
        {
            return _left.Dequeue();
        }

        return _right.Dequeue();
    }

    public bool Contains(int pageID)
    {
        return _left.Contains(pageID) || _right.Contains(pageID);
    }
}