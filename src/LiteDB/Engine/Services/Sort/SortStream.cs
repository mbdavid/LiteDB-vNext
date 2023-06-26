using System;

namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class SortStream : ISortStream
{
    private readonly Stream _stream;

    public SortStream(Stream stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Write a container sort data into stream. Buffer length should be less than CONTAINER_SORT_SIZE
    /// </summary>
    public ValueTask WriteContainer(int containerID, Memory<byte> buffer)
    {
        _stream.Position = containerID * CONTAINER_SORT_SIZE;

        return _stream.WriteAsync(buffer);
    }

    public ValueTask<int> ReadPage(int containerID, int pageID, PageBuffer page)
    {
        _stream.Position = containerID * CONTAINER_SORT_SIZE + (pageID * PAGE_SIZE);

        return _stream.ReadAsync(page.Buffer);
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}
