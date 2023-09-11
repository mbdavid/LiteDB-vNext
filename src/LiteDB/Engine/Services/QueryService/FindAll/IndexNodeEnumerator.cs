﻿namespace LiteDB.Engine;

unsafe internal class IndexNodeEnumerator : IEnumerator<IndexNodeResult>
{
    private readonly IIndexService _indexService;
    private readonly IndexDocument _indexDocument;

    private IndexNodeResult _current;
    private bool _init = false;
    private bool _eof = false;

    public IndexNodeEnumerator(IIndexService indexService, IndexDocument indexDocument)
    {
        _indexService = indexService;
        _indexDocument = indexDocument;

        _current = IndexNodeResult.Empty;
    }

    public IndexNodeResult Current => _current;
    object IEnumerator.Current => _current;

    public bool MoveNext()
    {
        if (_eof) return false;

        if (!_init)
        {
            _init = true;

            var head = _indexService.GetNode(_indexDocument.HeadIndexNodeID);

            if (head[0]->NextID == _indexDocument.TailIndexNodeID) return false;

            _current = _indexService.GetNode(head[0]->NextID);

            return true;
        }

        if (_current[0]->NextID == _indexDocument.TailIndexNodeID)
        {
            _eof = true;
            return false;
        }

        _current = _indexService.GetNode(_current[0]->NextID);

        return true;
    }

    public void Reset()
    {
        _current = IndexNodeResult.Empty;
        _init = false;
        _eof = false;
    }

    public void Dispose()
    {
        this.Reset();
    }
}
