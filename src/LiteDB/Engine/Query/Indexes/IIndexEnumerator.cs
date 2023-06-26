namespace LiteDB.Engine;

internal interface IIndexEnumerator
{
    ValueTask<PageAddress> MoveNextAsync(IIndexService indexService);
}
