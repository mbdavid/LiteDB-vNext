namespace LiteDB.Engine;

/// <summary>
/// This class monitor all open transactions to manage memory usage for each transaction
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class MonitorService : IMonitorService
{
    // dependency injection
    private readonly IServicesFactory _factory;

    // concurrent data-structures
    private readonly ConcurrentDictionary<int, ITransaction> _transactions = new();

    //private readonly ConcurrentDictionary<int, object> _openCursors = new();

    private int _lastTransactionID = 0;

    // expose open transactions
    public ICollection<ITransaction> Transactions => _transactions.Values;

    public MonitorService(IServicesFactory factory)
    {
        _factory = factory;
    }

    public async Task<ITransaction> CreateTransactionAsync(byte[] writeCollections, int readVersion = -1)
    {
        var transactionID = Interlocked.Increment(ref _lastTransactionID);
        var transaction = _factory.CreateTransaction(transactionID, writeCollections, readVersion);

        _transactions.TryAdd(transactionID, transaction);

        await transaction.InitializeAsync();

        return transaction;
    }

    /// <summary>
    /// </summary>
    public void ReleaseTransaction(ITransaction transaction)
    {
        // dispose current transaction
        transaction.Dispose();

        // remove from "open transaction" list
        _transactions.TryRemove(transaction.TransactionID, out _);
    }

    /// <summary>
    /// Check if transaction size reach limit AND check if is possible extend this limit
    /// </summary>
    public bool CheckSafepoint(Transaction trans)
    {
        return false; //TODO: implementar o momento de fazer safepoint
//            trans.Pages.TransactionSize >= trans.MaxTransactionSize &&
//            this.TryExtend(trans) == false;
    }

    /// <summary>
    /// Dispose all open transactions
    /// </summary>
    public void Dispose()
    {
        if (_transactions.Count > 0)
        {
            foreach (var transaction in _transactions.Values)
            {
                transaction.Dispose();
            }

            _transactions.Clear();
        }
    }
}