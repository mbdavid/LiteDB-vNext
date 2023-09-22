namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    internal ValueTask<EngineResult> ExecuteAsync(IScalarStatement statement, BsonDocument parameters)
    {
        throw new NotImplementedException();
    }
}
