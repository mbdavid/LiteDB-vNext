namespace LiteDB;

internal partial interface IServicesFactory
{
    EngineState State { get; set; }
}

//{
//    #region Engine Services/Settings

//    public EngineSettings Settings { get; }

//    public EngineState State { get; private set; }

//    public MemoryCacheService Cache { get; private set; }

//    // demais serviços

//    #endregion

//    public ServicesFactory(EngineSettings settings)
//    {
//        if (settings == null) throw new ArgumentNullException(nameof(settings));

//        this.Settings = settings;
//    }

//    #region Interfaces class activators

//    //public IMemoryCacheService CreateMemoryCacheService()
//    //    => new MemoryCacheService();

//    //public IOpenCommand CreateOpenCommand()
//    //    => new OpenCommand(this);

//    #endregion
//}

