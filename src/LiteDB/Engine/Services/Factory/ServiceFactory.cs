namespace LiteDB;

internal partial interface IServicesFactory
{
    IDiskStream CreateDiskStream(EngineSettings settings, bool sequencial);
}

internal partial class ServicesFactory : IServicesFactory
{
    public IDiskStream CreateDiskStream(EngineSettings settings, bool sequencial)
    {
        if (settings.Filename is null) throw new NotImplementedException();

        return new FileDiskStream(
            settings.Filename, 
            settings.Password, 
            settings.ReadOnly, 
            sequencial);
    }
}