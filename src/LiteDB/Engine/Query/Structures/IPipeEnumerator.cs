namespace LiteDB.Engine;

/// <summary>
/// Interface for a custom query pipe
/// </summary>
internal interface IPipeEnumerator
{
    ValueTask<PipeValue> MoveNextAsync(PipeContext context);
}
