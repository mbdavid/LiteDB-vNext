namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    /// <summary>
    /// Parse and execute an ah-hoc sql statement
    /// </summary>
    public ValueTask<EngineResult> ExecuteScalarAsync(string sql, BsonDocument parameters)
    {
        var collation = _factory.FileHeader.Collation;
        var tokenizer = new Tokenizer(sql);

        var parser = new SqlParser(tokenizer, collation);

        var statement = parser.ParseStatement();

        return this.ExecuteScalarAsync(statement, parameters);
    }

    /// <summary>
    /// Execute a prepared scalar statement using parameters. Catch any error and return all information about execution in EngineResult
    /// </summary>
    internal async ValueTask<EngineResult> ExecuteScalarAsync(IScalarStatement statement, BsonDocument parameters)
    {
        try
        {
            var result = await statement.ExecuteScalarAsync(_factory, parameters);

            return new EngineResult(result);
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle(ex, _factory, true);

            return new EngineResult(ex);
        }
    }
}
