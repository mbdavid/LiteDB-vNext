namespace LiteDB.Engine;

/// <summary>
/// Interface factory to provider new Stream instances for datafile/walfile resources. It's useful to multiple threads can read same datafile
/// </summary>
internal interface IStreamFactory
{
    /// <summary>
    /// Get Stream name (filename)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Get new Stream instance
    /// </summary>
    Stream GetStream(bool readOnly);

    /// <summary>
    /// Get file length
    /// </summary>
    long GetLength();

    /// <summary>
    /// Delete physical file on disk
    /// </summary>
    void Delete();

    /// <summary>
    /// Indicate that factory must be dispose on finish
    /// </summary>
    bool CloseOnDispose { get; }
}
