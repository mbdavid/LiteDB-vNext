namespace LiteDB.Engine;

internal class TransformEnumerator : IPipeEnumerator
{
    private readonly SelectFields _fields;
    private readonly Collation _collation;
    private readonly IPipeEnumerator _enumerator;

    private bool _eof = false;

    public TransformEnumerator(SelectFields fields, Collation collation, IPipeEnumerator enumerator)
    {
        _fields = fields;
        _enumerator = enumerator;
        _collation = collation;

        if (_enumerator.Emit.Document == false) throw ERR($"Transform pipe enumerator requires document from last pipe");
    }

    public PipeEmit Emit => new(indexNodeID: _enumerator.Emit.IndexNodeID, dataBlockID: _enumerator.Emit.DataBlockID, document: true);

    public PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var item = _enumerator.MoveNext(context);

        if (item.IsEmpty)
        {
            _eof = true;
            return PipeValue.Empty;
        }

        //TODO: otimizar essa criação de um novo documento, pois pode chegar o item.Document já pronto
        // ou seja, pode ser que não seja necessario fazer nada aqui

        if (_fields.IsSingleExpression)
        {
            var value = _fields.SingleExpression.Execute(item.Document, context.QueryParameters, _collation);

            return new PipeValue(item.IndexNodeID, item.DataBlockID, value.AsDocument);
        }
        else
        {
            var doc = new BsonDocument();

            foreach (var field in _fields.Fields)
            {
                // get field expression value
                var value = field.Expression.Execute(item.Document, context.QueryParameters, _collation);

                // and add to final document
                doc.Add(field.Name, value);
            }

            return new PipeValue(item.IndexNodeID, item.DataBlockID, doc);
        }
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"TRANSFORM {_fields}", deep);

        _enumerator.GetPlan(builder, ++deep);
    }

    public void Dispose()
    {
    }
}
