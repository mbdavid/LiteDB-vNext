namespace LiteDB.Engine;

/// <summary>
/// Lock service are collection-based locks. Lock will support any threads reading at same time. Writing operations will be locked
/// based on collection. Eventualy, write operation can change header page that has an exclusive locker for.
/// [ThreadSafe]
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class LockService : ILockService
{
    private readonly AsyncReaderWriterLock _database;
    private readonly AsyncReaderWriterLock[] _collections;

    public LockService(TimeSpan timeout)
    {
        // allocate main database async locker
        _database = new AsyncReaderWriterLock(timeout);

        // allocate all 255 possible collections
        _collections = Enumerable.Range(0, byte.MaxValue + 1)
            .Select(x => new AsyncReaderWriterLock(timeout))
            .ToArray();
    }

    /// <summary>
    /// Return how many transactions are opened
    /// </summary>
    public int TransactionsCount => _database.ReaderCount;

    /// <summary>
    /// All non-exclusive database operations must call this EnterTranscation() just before working. 
    /// This will be used to garantee exclusive write-only (non-reader) during exclusive operations (like checkpoint)
    /// </summary>
    public async Task EnterTransactionAsync()
    {
        await _database.AcquireReaderLock();
    }

    /// <summary>
    /// Exit transaction read lock
    /// </summary>
    public void ExitTransaction()
    {
        _database.ReleaseReaderLock();
    }

    /// <summary>
    /// Enter all database in exclusive lock. Wait for all transactions finish. In exclusive mode no one can enter in new transaction (for read/write)
    /// If current thread already in exclusive mode, returns false
    /// </summary>
    public async Task EnterExclusiveAsync()
    {
        await _database.AcquireWriterLock();
    }

    /// <summary>
    /// Exit exclusive lock
    /// </summary>
    public void ExitExclusive()
    {
        _database.ReleaseWriterLock();
    }

    /// <summary>
    /// Enter collection write lock mode (only 1 collection per time can have this lock)
    /// </summary>
    public async Task EnterCollectionWriteLockAsync(byte colID)
    {
        await _collections[colID].AcquireWriterLock();
    }

    /// <summary>
    /// Exit collection in reserved lock
    /// </summary>
    public void ExitCollectionWriteLock(byte colID)
    {
        _collections[colID].ReleaseWriterLock();
    }


    public void Dispose()
    {
        try
        {
            _database.Dispose();

            foreach (var collection in _collections)
            {
                collection.Dispose();
            }
        }
        catch (SynchronizationLockException)
        {
        }
    }
}