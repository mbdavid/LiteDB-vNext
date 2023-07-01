using System.ComponentModel;

namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class SortContainer : ISortContainer
{
    // dependency injections
    private readonly IBufferFactory _bufferFactory;
    private readonly Collation _collation;
    private readonly Stream _stream;

    private readonly int _containerID;
    private readonly int _containerSizeInPages;
    private PageBuffer _buffer;

    private SortItem _current; // current sorted item
    private int _pageIndex = -1; // current read page
    private int _position = 0; // current read position

    private int _containerRemaining; // remaining items on this container 
    private int _pageRemaining; // remaining items on current page

    public SortContainer(
        IBufferFactory bufferFactory,
        Collation collation,
        Stream stream,
        int containerID, 
        int containerSizeInPages)
    {
        _bufferFactory = bufferFactory;
        _stream = stream;
        _collation = collation;
        _containerID = containerID;
        _containerSizeInPages = containerSizeInPages;

        _buffer = bufferFactory.AllocateNewPage(false);
    }

    /// <summary>
    /// Get container ID on disk
    /// </summary>
    public int ContainerID => _containerID;

    /// <summary>
    /// Get current readed item
    /// </summary>
    public SortItem Current => _current;

    /// <summary>
    /// Get how many items are not readed from container (if 0 all container already readed)
    /// </summary>
    public int Remaining => _containerRemaining;

    /// <summary>
    /// Sort all unsorted items based on order. Write all bytes into buffer only. 
    /// Organized all items in 8k pages, with first 2 bytes to contains how many items this page contains
    /// Remaining items are not inserted in this container e must be returned to be added into a new container
    /// </summary>
    public void Sort(IEnumerable<SortItem> unsortedItems, int order, byte[] containerBuffer, List<SortItem> remaining)
    {
        // order items
        var query = order == Query.Ascending ?
            unsortedItems.OrderBy(x => x.Key, _collation) : unsortedItems.OrderByDescending(x => x.Key, _collation);

        var pagePosition = 2; // first 2 bytes per page was used to store how many item will contain this page
        short pageItems = 0;
        var pageCount = 0;

        var span = containerBuffer.AsSpan(0, PAGE_SIZE);

        foreach (var orderedItem in query)
        {
            var itemSize = orderedItem.GetBytesCount();

            // test if this new 
            if (pagePosition + itemSize > PAGE_SIZE)
            {
                // use first 2 bytes to store how many sort items this page has
                span[0..].WriteInt16(pageItems);

                pageItems = 0;
                pagePosition = 2;
                pageCount++;

                // define span as new page
                span = containerBuffer.AsSpan(pageCount * PAGE_SIZE, PAGE_SIZE);
            }

            // if need more pages than _containerSizeInPages, add to "remaining" list to be added in another container
            if (pageCount >= _containerSizeInPages)
            {
                remaining.Add(orderedItem);
            }
            else
            {
                // write RowID, Key on buffer
                span[pagePosition..].WritePageAddress(orderedItem.RowID);

                pagePosition += PageAddress.SIZE;

                span[pagePosition..].WriteBsonValue(orderedItem.Key, out var keyLength);

                pagePosition += keyLength;

                // move to next item
                pagePosition += itemSize;

                // increment total container items
                _containerRemaining++;
            }
        }
    }

    /// <summary>
    /// Move "Current" to next item on this container. Returns false if eof
    /// </summary>
    public async ValueTask<bool> MoveNextAsync()
    {
        if (_containerRemaining == 0) return false;

        if (_pageRemaining == 0)
        {
            // set stream position to page position (increment pageIndex before)
            _stream.Position = (_containerID * (_containerSizeInPages * PAGE_SIZE)) + (++_pageIndex * PAGE_SIZE);

            await _stream.ReadAsync(_buffer.Buffer);

            // set position and read remaining page items
            _position = _pageIndex * PAGE_SIZE;
            _pageRemaining = _buffer.AsSpan(0, 2).ReadInt16();
        }

        var itemSize = this.ReadCurrent();

        _position += itemSize;
        _pageRemaining--;
        _containerRemaining--;

        return true;
    }

    /// <summary>
    /// Read current item on buffer and return item length
    /// </summary>
    private int ReadCurrent()
    {
        var span = _buffer.AsSpan(_position);

        var rowID = span[0..].ReadPageAddress();
        var key = span[PageAddress.SIZE..].ReadBsonValue(out var keyLength);

        // set current item
        _current = new SortItem(rowID, key);

        return PageAddress.SIZE + keyLength;
    }

    public void Dispose()
    {
        _bufferFactory.DeallocatePage(_buffer);
    }
}
