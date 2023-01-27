namespace LiteDB.Engine;

internal interface IMemoryCachePage
{
    BasePage Page { get; }
    int ShareCounter { get; }
    long Timestamp { get; }

    void Rent();
    void Return();
}