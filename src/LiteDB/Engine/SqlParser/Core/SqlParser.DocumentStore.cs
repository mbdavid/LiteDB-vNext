namespace LiteDB.Engine;

internal partial class SqlParser
{
    private bool TryParseDocumentStore(out IDocumentStore store)
    {
        var ahead = _tokenizer.LookAhead();

        if (ahead.Type == TokenType.Word) // user_collection
        {
            var token = _tokenizer.ReadToken(); // read "collection_name";

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
}
