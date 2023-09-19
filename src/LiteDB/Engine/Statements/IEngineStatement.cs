namespace LiteDB.Engine;

internal interface IEngineStatement
{
    ValueTask<int> Execute(IServicesFactory factory);
}
