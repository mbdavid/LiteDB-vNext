namespace LiteDB.Engine;

internal interface IDiskStream : IDisposable
{
    string Name { get; }
    bool Exists();
    long GetLength();
    void Delete();
    Task FlushAsync();
    Task<bool> ReadPageAsync(long position, PageBuffer buffer);
    Task WritePageAsync(PageBuffer buffer);
}
