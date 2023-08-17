namespace LiteDB.Engine;

internal class IncludeEnumerator : IPipeEnumerator
{
    // dependency injections
    private readonly IMasterService _masterService;
    private readonly Collation _collation;

    private readonly BsonExpression _pathExpr;
    private readonly IPipeEnumerator _enumerator;

    private bool _eof = false;

    public IncludeEnumerator(
        BsonExpression pathExpr, 
        IPipeEnumerator enumerator, 
        IMasterService masterService, 
        Collation collation)
    {
        _pathExpr = pathExpr;
        _enumerator = enumerator;
        _masterService = masterService;
        _collation = collation;

        if (_enumerator.Emit.RowID == false) throw ERR($"Include pipe enumerator requires document from last pipe");
    }

    public PipeEmit Emit => new(_enumerator.Emit.RowID, true);

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var item = await _enumerator.MoveNextAsync(context);

        if (item.IsEmpty)
        {
            _eof = true;

            return PipeValue.Empty;
        }

        var value = _pathExpr.Execute(item.Document, context.QueryParameters, _collation);

        if (value.IsDocument)
        {
            await this.DoIncludeAsync(value.AsDocument, context);
        }
        else if (value.IsArray)
        {
            var array = value.AsArray;

            foreach(var elem in array) 
            {
                if (!elem.IsDocument) continue;

                await this.DoIncludeAsync(elem.AsDocument, context);
            }
        }

        return item;
    }

    /// <summary>
    /// Do include changes inner document instance to add all fields from that collection using _id
    /// </summary>
    private async ValueTask DoIncludeAsync(BsonDocument value, PipeContext context)
    {
        // works only if is a document
        var refId = value["$id"];
        var refCol = value["$ref"];

        // if has no reference, just go out
        if (refId.IsNull || !refCol.IsString) return;

        // get master to get collection PK index
        var master = _masterService.GetMaster(false);

        if (master.Collections.TryGetValue(refCol.AsString, out var collection))
        {
            var (indexNode, _) = await context.IndexService.FindAsync(collection.PK, refId, false, Query.Ascending);

            if (!indexNode.IsEmpty)
            {
                var refDocResult = await context.DataService.ReadDocumentAsync(indexNode.DataBlock, Array.Empty<string>());

                if (refDocResult.Fail) throw refDocResult.Exception;

                //do not remove $id
                value.Remove("$ref");

                // copy values from refDocument into current documet (except _id - will keep $id)
                foreach (var element in refDocResult.Value.AsDocument.Where(x => x.Key != "_id"))
                {
                    value[element.Key] = element.Value;
                }
            }
            else
            {
                // set in ref document that was not found
                value["$missing"] = true;
            }
        }
        else
        {
            // set in ref document that was not found
            value["$missing"] = true;
        }
    }

    public void Dispose()
    {
    }
}
