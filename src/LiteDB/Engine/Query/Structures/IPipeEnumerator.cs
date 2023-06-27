namespace LiteDB.Engine;

/// <summary>
/// Interface for a custom query pipe
/// </summary>
internal interface IPipeEnumerator : IDisposable
{
    ValueTask<PipeValue> MoveNextAsync(PipeContext context);
}
