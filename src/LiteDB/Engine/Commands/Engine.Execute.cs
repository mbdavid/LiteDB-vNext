namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    /// <summary>
    /// Parse and execute an ah-hoc sql statement
    /// </summary>
    public ValueTask<int> ExecuteAsync(string sql, BsonDocument parameters)
    {
        var collation = _factory.FileHeader.Collation;
        var tokenizer = new Tokenizer(sql);
        var parser = new SqlParser(tokenizer, collation);

        try
        {
            var statement = parser.ParseStatement();

            var result = statement.ExecuteAsync(_factory, parameters);

            return result;

        }
        catch (Exception ex)
        {
            ex.HandleException(_factory);
            throw;
        }
    }
}
