namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    private IScalarStatement ParseInsert()
    {
        _tokenizer.ReadToken().Expect("INSERT");
        _tokenizer.ReadToken().Expect("INTO");

        if (!this.TryParseDocumentStore(out var store)) throw ERR_UNEXPECTED_TOKEN(_tokenizer.Current, "{document_store}");

        TryParseWithAutoId(out var autoId);

        _tokenizer.ReadToken().Expect("VALUES");

        var ahead = _tokenizer.LookAhead();

        if (ahead.Type == TokenType.At) // @0 - is expression parameter
        {
            var docExpr = BsonExpression.Create(_tokenizer, false);

            var statement = new InsertStatement(store, docExpr, autoId);

            return statement;
        }
        else if (ahead.Type == TokenType.OpenBrace) // { new json document
        {
            var doc = JsonReaderStatic.ReadDocument(_tokenizer); // read full json_document

            var statement = new InsertStatement(store, doc, autoId);

            return statement;
        }
        else if (ahead.Type == TokenType.OpenBracket) // [ new json array
        {
            var array = JsonReaderStatic.ReadArray(_tokenizer); // read full json_array

            var statement = new InsertStatement(store, (IReadOnlyList<BsonDocument>)array.Value, autoId);

            return statement;
        }
        else if (ahead.Type == TokenType.OpenParenthesis) // ( new sub_query
        {
            throw new NotImplementedException("sub_query");
        }

        throw new NotImplementedException();
    }
}
