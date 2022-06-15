namespace LiteDB.Engine;

/// <summary>
/// 
/// </summary>
public interface ILiteEngine : IDisposable
{
    /// <summary>
    /// Descreve em qual situação o banco está
    /// 0 - Fechado
    /// 1 - Ligando banco
    /// 2 - Banco ligado e operando
    /// 3 - Em desligamento
    /// 99 - Falha
    /// </summary>
    int State { get; }

    /// <summary>
    /// Retorna a exception que tirou o banco do ar (null caso ok e limpa no open())
    /// </summary>
    Exception FatalException { get; }

    /// <summary>
    /// abre os arquivos, carrega header, faz recovery, e deixa o estado do banco pronto pra uso
    /// </summary>
    Task OpenAsync();

    Task ShutdownAsync(bool forced, Exception exception);

    /*

        Task<bool> CreateCollectionAsync(string name, object options);
        Task<bool> DropCollectionAsync(string name);

        /// <summary>
        /// Retorna o _id
        /// </summary>
        Task<BsonValue> InsertAsync(string collection, BsonDocument document, int autoId);
        Task<bool> UpdateAsync(string collection, BsonValue id, BsonExpression modify);
        Task<bool> DeleteAsync(string collection, BsonValue id);

        Task<object> BulkAsync(IList<object> operations);

        Task<bool> CreateIndexAsync(string collection, string name, BsonExpression expression, bool unique);
        Task<bool> DropIndexAsync(string collection, string name);

        Task<Guid> QueryAsync(string collection, object query, int buffer = 1024);
        Task<Guid> FetchAsync(Guid cursorId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forced">Invalidate all pending cursors.</param>
        /// <returns></returns>
        Task<int> CheckpointAsync(bool forced);
        Task<int> RebuildAsync(object options);
        Task<bool> SetPragmaAsync(string name, object value);
    */

}
