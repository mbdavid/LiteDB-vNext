﻿namespace LiteDB.Engine;

internal class FilterEnumerator : IPipeEnumerator
{
    // dependency injections
    private readonly Collation _collation;

    private readonly IPipeEnumerator _enumerator;
    private readonly BsonExpression _filter;

    private bool _eof = false;

    public FilterEnumerator(BsonExpression filter, IPipeEnumerator enumerator, Collation collation)
    {
        _filter = filter;
        _enumerator = enumerator;
        _collation = collation;

        if (_enumerator.Emit.Document == false) throw ERR($"Filter pipe enumerator requires document from last pipe");
    }

    public PipeEmit Emit => new(_enumerator.Emit.DataBlockID, true);

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        while (!_eof)
        {
            var item = await _enumerator.MoveNextAsync(context);

            if (item.IsEmpty)
            {
                _eof = true;
            }
            else
            {
                var result = _filter.Execute(item.Document, context.QueryParameters, _collation);

                if (result.IsBoolean && result.AsBoolean)
                {
                    return item;
                }
            }
        }

        return PipeValue.Empty;
    }

    public void Dispose()
    {
    }
}
