namespace LiteDB.Engine;

internal interface IDiskStream : IDisposable
{
    string Name { get; }
    bool Exists();
    long GetLength();
    void Delete();
    Task<FileHeader> OpenAsync();
    Task CreateAsync(FileHeader fileHeader);
    Task FlushAsync();
    Task<bool> ReadPageAsync(long position, PageBuffer buffer);
    Task WritePageAsync(PageBuffer buffer);
}
