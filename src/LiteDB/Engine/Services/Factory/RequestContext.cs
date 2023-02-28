namespace LiteDB;

internal class RequestContext
{
    public Dictionary<string, BsonValue> Meta = new();

    public IServicesFactory Factory { get; }

    public RequestContext(IServicesFactory factory)
    {
        this.Factory = factory;
    }
}
