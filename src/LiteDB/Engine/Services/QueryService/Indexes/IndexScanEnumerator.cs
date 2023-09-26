﻿namespace LiteDB.Engine;

internal class IndexScanEnumerator : IPipeEnumerator
{
    private readonly IndexDocument _indexDocument;
    private readonly Func<BsonValue, bool> _func;
    private readonly int _order;

    private bool _init = false;
    private bool _eof = false;

    private RowID _next = RowID.Empty; // all nodes from right of first node found

    public IndexScanEnumerator(
        IndexDocument indexDocument,
        Func<BsonValue, bool> func,
        int order)
    {
        _indexDocument = indexDocument;
        _func = func;
        _order = order;
    }

    public PipeEmit Emit => new(indexNodeID: true, dataBlockID: true, document: false);

    public unsafe PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        var head = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;
        var tail = _order == Query.Ascending ? _indexDocument.TailIndexNodeID : _indexDocument.HeadIndexNodeID;

        // in first run, gets head node
        if (_init == false)
        {
            _init = true;

            var node = indexService.GetNode(head);

            // get pointer to next at level 0
            _next = node[0]->GetNext(_order);

            // empty index
            if (_next == tail)
            {
                _eof = true;
                return PipeValue.Empty;
            }
        }

        // loop until find any func<> = true
        while (true)
        {
            var node = indexService.GetNode(_next);

            _next = node[0]->GetNext(_order);

            // if next is tail, do not run more than this time
            if (_next == tail) _eof = true;

            // get key as BsonValue to run computed function
            var key = IndexKey.ToBsonValue(node.Key);

            if (_func(key))
            {
                return new PipeValue(node.IndexNodeID, node.DataBlockID);
            }
        } 
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"INDEX FULL SCAN \"{_indexDocument.Name}\"", deep);
    }

    public void Dispose()
    {
    }
}
