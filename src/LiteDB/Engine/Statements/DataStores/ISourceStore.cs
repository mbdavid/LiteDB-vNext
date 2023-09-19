namespace LiteDB.Engine;

internal interface ISourceStore
{
    byte ColID { get; }

    string Name { get; }

//    void Load(IMasterService masterService);
    IReadOnlyList<IndexDocument> Indexes { get; }

    IndexDocument PK => Indexes[0];

    IPipeEnumerator GetPipeEnumerator(BsonExpression expression);
}
