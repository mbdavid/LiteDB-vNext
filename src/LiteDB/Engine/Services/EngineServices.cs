namespace LiteDB.Engine;

/// <summary>
/// Represent all services needed on LiteEngine. Contains a single instance for each engine instance
/// </summary>
internal class EngineServices : IDisposable
{
    public EngineServices Settings { get; }

    public HeaderPage Header { get; }

    public IStreamFactory StreamFactory { get; }

    public EngineState State { get; }

    public object CreateTransaction()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}
