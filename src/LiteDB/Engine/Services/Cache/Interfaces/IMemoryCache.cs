namespace LiteDB.Engine;

internal interface IMemoryCache
{
    void AddPage(long position, BasePage page);
    int CleanUp();
    IMemoryCachePage GetPage(long position);
    void ReturnPage(long position);
}
