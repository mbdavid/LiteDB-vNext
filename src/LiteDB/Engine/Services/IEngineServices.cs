namespace LiteDB.Engine;

internal interface IEngineServices : IDisposable
{
    EngineState State { get; }

    IMemoryCache Cache { get; }
    AsyncReaderWriterLock Locker { get; }

    Task OpenAsync(CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
    
}

