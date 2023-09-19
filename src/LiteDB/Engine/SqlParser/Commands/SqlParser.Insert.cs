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

        if (!this.TryParseDocumentStore(out var store)) throw ERR_UNEXPECTED_TOKEN("Collection name expected", _tokenizer.Current);

        TryParseWithAutoId(out var autoId);

        _tokenizer.ReadToken().Expect("VALUES");

        var ahead = _tokenizer.LookAhead();

        if (ahead.Type == TokenType.At) // @0 - is parameter
        {
            var docExpr = BsonExpression.Create(_tokenizer, true);

            var statement = new InsertStatement(store, docExpr, autoId);


            return statement;


        }
        else if (ahead.Type == TokenType.OpenBrace) // { new document
        {
            var doc = JsonReaderStatic.Deserialize(_tokenizer);

            var statement = new InsertStatement(store, doc, autoId);

            return statement;
        }

        throw new NotImplementedException();

    }

    private bool TryParseDocumentStore(out IDocumentStore store)
    {
        throw new NotImplementedException();
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

            if (type.Value.Equals("GUID", StringComparison.OrdinalIgnoreCase))
                autoId = BsonAutoId.Guid;
            else if (type.Value.Equals("INT", StringComparison.OrdinalIgnoreCase))
                autoId = BsonAutoId.Int32;
            else if (type.Value.Equals("LONG", StringComparison.OrdinalIgnoreCase))
                autoId = BsonAutoId.Int64;
            else if (type.Value.Equals("OBJECTID", StringComparison.OrdinalIgnoreCase))
                autoId = BsonAutoId.ObjectId;
            else
                throw ERR_UNEXPECTED_TOKEN(type, "GUID, INT, LONG, OBJECTID");

            return true;
        }

        autoId = BsonAutoId.Int32;

        return false;
    }

}
