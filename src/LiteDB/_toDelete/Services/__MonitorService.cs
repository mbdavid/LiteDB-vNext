﻿namespace LiteDB.Engine;

/// <summary>
/// This class monitor all open transactions to manage memory usage for each transaction
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class __MonitorService : I__MonitorService
{
    // dependency injection
    private readonly I__ServicesFactory _factory;

    // concurrent data-structures
    private readonly ConcurrentDictionary<int, I__Transaction> _transactions = new();

    //private readonly ConcurrentDictionary<int, object> _openCursors = new();

    private int _lastTransactionID = 0;

    // expose open transactions
    public ICollection<I__Transaction> Transactions => _transactions.Values;

    public __MonitorService(I__ServicesFactory factory)
    {
        _factory = factory;
    }

    public async ValueTask<I__Transaction> CreateTransactionAsync(int readVersion)
    {
        var transactionID = Interlocked.Increment(ref _lastTransactionID);
        var transaction = _factory.CreateTransaction(transactionID, Array.Empty<byte>(), readVersion);

        _transactions.TryAdd(transactionID, transaction);

        await transaction.InitializeAsync();

        return transaction;
    }

    public async ValueTask<I__Transaction> CreateTransactionAsync(byte[] writeCollections)
    {
        var transactionID = Interlocked.Increment(ref _lastTransactionID);
        var transaction = _factory.CreateTransaction(transactionID, writeCollections, -1);

        _transactions.TryAdd(transactionID, transaction);

        await transaction.InitializeAsync();

        return transaction;
    }

    /// <summary>
    /// </summary>
    public void ReleaseTransaction(I__Transaction transaction)
    {
        // dispose current transaction
        transaction.Dispose();

        // remove from "open transaction" list
        _transactions.TryRemove(transaction.TransactionID, out _);
    }

    /// <summary>
    /// Check if transaction size reach limit AND check if is possible extend this limit
    /// </summary>
    public bool Safepoint(I__Transaction transaction)
    {
        return transaction.PagesUsed > SAFEPOINT_SIZE; //TODO: implementar o momento de fazer safepoint
//            trans.Pages.TransactionSize >= trans.MaxTransactionSize &&
//            this.TryExtend(trans) == false;
    }

    public override string ToString()
    {
        return Dump.Object(new { _transactions });
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