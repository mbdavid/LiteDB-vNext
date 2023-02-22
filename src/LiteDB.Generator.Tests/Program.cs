namespace LiteDB;

public class Program
{
    public static void Main(string[] args)
    {
        IServicesFactory factory = new ServicesFactory();

        var disk = factory.CreateDiskService();

        disk.RendStreamReader();

    }
}


[AutoInterface(true)]
public class DiskService : IDiskService
{
    private readonly IServicesFactory _factory;

    public DiskService(IServicesFactory factory)
    {
        _factory = factory;
        _factory.CreateStreamPool(10);
    }

    public Stream RendStreamReader()
    {
        throw new NotImplementedException();
    }

    public void ReturnReader(Stream stream)
    {
    }
}

[AutoInterface(true)]
public class StreamPool : IStreamPool
{
    public StreamPool(int limit)
    {
    }

    public Stream RendStreamReader()
    {
        return new MemoryStream();
    }

    public void ReturnReader(Stream stream)
    {
    }

}