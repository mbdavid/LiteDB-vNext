namespace LiteDB.Engine;

/// <summary>
/// Disk file reader - must call Dipose after use to return reader into pool
/// This class is not ThreadSafe - must have 1 instance per thread (get instance from DiskService)
/// </summary>
internal class DiskReader : IDisposable
{
    private readonly StreamPool _streamPool;

    private readonly Lazy<Stream> _stream;

    public DiskReader(StreamPool streamPool)
    {
        _streamPool = streamPool;

        // use lazy because you can have transaction that will read only from cache
        _stream = new Lazy<Stream>(() => _streamPool.Rent());
    }

    /// <summary>
    /// Read a page direct from disk. Do not use memory or cache. Returns false if page doesn't exists
    /// </summary>
    public async Task<bool> ReadPageAsync(Memory<byte> buffer, long position, CancellationToken cancellationToken)
    {
        ENSURE(buffer.Length == PAGE_SIZE, $"Page memory buffer must have {PAGE_SIZE} length");

        var stream = _stream.Value;

        stream.Position = position;

        var read = await stream.ReadAsync(buffer, cancellationToken);

        if (read == 0) return false;

        ENSURE(read == PAGE_SIZE, $"Page read in disk must returns {PAGE_SIZE} read bytes");

        // validação de CRC8 aqui? E quando a pagina é vazia?

        return true;
    }

    /// <summary>
    /// When dispose, return stream to pool
    /// </summary>
    public void Dispose()
    {
        if (_stream.IsValueCreated)
        {
            _streamPool.Return(_stream.Value);
        }
    }
}
