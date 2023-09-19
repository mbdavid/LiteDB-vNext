namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    internal ValueTask<EngineResult> ExecuteAsync(IEngineStatement statement, BsonDocument parameters)
    {
        throw new NotImplementedException();
    }
}
