using static System.Formats.Asn1.AsnWriter;

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

        if (!this.TryParseDocumentStore(out var store)) throw ERR_UNEXPECTED_TOKEN(_tokenizer.Current, "<document_store>");

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

    private bool TryParseDocumentStore(out IDocumentStore store)
    {
        var ahead = _tokenizer.LookAhead();

        if (ahead.Type == TokenType.Word) // user_collection
        {
            var token = _tokenizer.ReadToken().Expect(TokenType.Word); // read "collection-name";

            store = new UserCollectionStore(token.Value);

            return true;
        }
        else if (ahead.Type != TokenType.Dollar)
        {
            _tokenizer.ReadToken(); // read "$";

            var token = _tokenizer.ReadToken().Expect(TokenType.Word); // read "collection-name"

            if (token.Type == TokenType.String)
            {
                if (TryParseParameters(out _))
                {
                    //TODO: verificar metodos
                }

            }

            throw new NotImplementedException();

            //return true;
        }

        store = default;
        return false;

    }

    /// <summary>
    /// Try read a list of bson-value parameters. Must starts with "("
    /// </summary>
    private bool TryParseParameters(out IReadOnlyList<BsonValue> parameters)
    {
        var ahead = _tokenizer.LookAhead();

        if (ahead.Type != TokenType.OpenParenthesis)
        {
            parameters = Array.Empty<BsonValue>();
            return false;
        }

        var token = _tokenizer.ReadToken(); // read "("

        var result = new List<BsonValue>();

        while (token.Type != TokenType.CloseParenthesis)
        {
            var value = JsonReaderStatic.ReadValue(_tokenizer);

            result.Add(value);

            token = _tokenizer.ReadToken(); // read <next>, "," or ")"

            if (token.Type == TokenType.Comma)
            {
                token = _tokenizer.ReadToken();
            }
        }

        parameters = result;
        return true;
    }


    /// <summary>
    /// Parse :[type] for AutoId (just after collection name)
    /// </summary>
    private bool TryParseWithAutoId(out BsonAutoId autoId)
    {
        var with = _tokenizer.LookAhead();

        if (with.Type == TokenType.Colon)
        {
            _tokenizer.ReadToken();

            var type = _tokenizer.ReadToken().Expect(TokenType.Word);

            if (type.Value.Eq("GUID"))
                autoId = BsonAutoId.Guid;
            else if (type.Value.Eq("INT"))
                autoId = BsonAutoId.Int32;
            else if (type.Value.Eq("LONG"))
                autoId = BsonAutoId.Int64;
            else if (type.Value.Eq("OBJECTID"))
                autoId = BsonAutoId.ObjectId;
            else
                throw ERR_UNEXPECTED_TOKEN(type, "GUID, INT, LONG, OBJECTID");

            return true;
        }

        autoId = BsonAutoId.Int32;

        return false;
    }

}
