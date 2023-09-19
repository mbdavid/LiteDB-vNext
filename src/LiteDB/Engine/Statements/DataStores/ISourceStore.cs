namespace LiteDB.Engine;

public interface ISourceStore
{
    string Name { get; }

//    void Load(IMasterService masterService);

    IPipeEnumerator GetPipeEnumerator(BsonExpression expression);
}
