namespace LiteDB.Engine;

/// <summary>
/// Represent all services needed on LiteEngine. Contains a single instance for each engine instance
/// </summary>
internal class EngineServices : IDisposable
{
    #region All singleton services & function services

    public EngineSettings Settings { get; }

    public HeaderPage Header { get; set; }

    public EngineState State { get; set; } = EngineState.Close;

    public MemoryCache Cache { get; set; }

    public DiskService Disk { get; set; }

    public object CreateSnapshot()
    {
        throw new NotImplementedException();
    }

    #endregion

    public EngineServices(EngineSettings settings)
    {
        this.Settings = settings;
    }

    public void Dispose()
    {
        this.Disk?.Dispose();

        this.Header = null;
        this.Cache = null;
        this.Disk = null;

        this.State = EngineState.Close;
    }
}
