namespace LiteDB.Engine;

internal interface IQueryOptimization
{
    IPipeEnumerator ProcessQuery();
}
