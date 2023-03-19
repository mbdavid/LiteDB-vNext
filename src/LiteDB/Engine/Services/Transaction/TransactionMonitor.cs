namespace LiteDB.Engine;

/// <summary>
/// This class monitor all open transactions to manage memory usage for each transaction
/// [Singleton - ThreadSafe]
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class TransactionMonitor : ITransactionMonitor
{
    // dependency injection
    private readonly IServicesFactory _factory;

    private readonly ConcurrentDictionary<int, ITransactionService> _transactions = new ConcurrentDictionary<int, ITransactionService>();

    private int _lastTransactionID = 0;

    // expose open transactions
    public ICollection<ITransactionService> Transactions => _transactions.Values;

    public TransactionMonitor(IServicesFactory factory)
    {
        _factory = factory;
    }

    public async Task<ITransactionService> CreateTransactionAsync()
    {
        var transactionID = Interlocked.Increment(ref _lastTransactionID);
        var transaction = _factory.CreateTransaction(transactionID);

        _transactions.TryAdd(transactionID, transaction);

        await transaction.InitializeAsync();

        return transaction;
    }

    /// <summary>
    /// </summary>
    public void ReleaseTransaction(TransactionService transaction)
    {
        // dispose current transaction
        transaction.Dispose();

        // remove from "open transaction" list
        _transactions.TryRemove(transaction.TransactionID, out _);

    }

    /// <summary>
    /// Check if transaction size reach limit AND check if is possible extend this limit
    /// </summary>
    public bool CheckSafepoint(TransactionService trans)
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