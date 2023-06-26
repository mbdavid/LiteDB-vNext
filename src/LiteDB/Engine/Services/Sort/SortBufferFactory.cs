namespace LiteDB.Engine;

internal class SortBufferFactory
{
    private readonly ConcurrentQueue<SortBuffer> _freeBuffers = new();

    private int _buffersAllocated = 0;

    public SortBuffer AllocateNewBuffer()
    {
        if (_freeBuffers.TryDequeue(out var buffer))
        {
            return buffer;
        }

        Interlocked.Increment(ref _buffersAllocated);

        return new SortBuffer();
    }

    public void DeallocateBuffer(SortBuffer buffer)
    {
        _freeBuffers.Enqueue(buffer);
    }
}
