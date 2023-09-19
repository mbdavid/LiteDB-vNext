namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public ValueTask<EngineResult> ExecuteAsync(IEngineStatement statement, BsonDocument parameters)
    {
        throw new NotImplementedException();
    }
}
