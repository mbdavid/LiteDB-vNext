namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public ValueTask<EngineResult> ExecuteFetchAsync(Guid cursorID, int fetchSize = 1000)
    {
        throw new NotImplementedException();
    }
}
