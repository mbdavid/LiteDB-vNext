namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class CursorService : ICursorService
{
    // dependency injections
    private readonly IServicesFactory _factory;

    public CursorService(IServicesFactory factory)
    {
        _factory = factory;
    }

    private ConcurrentDictionary<Guid, Cursor> _openCursors = new();
    private ConcurrentDictionary<Guid, Cursor> _runningCursors = new();

    private int _minReadVersion = 0;
    private int _maxReadVersion = 0;

    public Guid CreateCursor()
    {
        var cursor = _factory.CreateCursor();

        return Guid.NewGuid();
    }

    public async ValueTask<FetchResult> FetchAsync(Guid cursorId)
    {
        // removo da _openCursos enquanto executo...
        // adiciono em _runningCursors;



        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}
