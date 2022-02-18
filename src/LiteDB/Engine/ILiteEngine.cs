namespace LiteDB.Engine;

/// <summary>
/// 
/// </summary>
public interface ILiteEngine : IAsyncDisposable
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

    Task OpenAsync();

    Task<bool> CreateCollection(string name, object options);
    Task<bool> DropCollection(string name);

    /// <summary>
    /// Retorna o _id
    /// </summary>
    Task<object> InsertAsync(string collection, object document, int autoId = 0);
    Task<object> UpdateAsync(string collection, object document);
    Task<int> UpsertAsync(string collection, object document, int autoId = 0);
    Task<object> DeleteAsync(string collection, object id);

    Task<object> BulkAsync(IEnumerable<object> operations, int bulkSize = 1000);

    Task<bool> CreateIndexAsync(string collection, string name, string expression, bool unique);
    Task<bool> DropIndexAsync(string collection, string name);

    Task<object> QueryAsync(string collection, object query, int buffer = 1024);
    Task<object> FetchAsync(Guid cursorId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="forced">Invalidate all pending cursors.</param>
    /// <returns></returns>
    Task<int> CheckpointAsync(bool forced);
    Task<int> RebuildAsync(object options);
    Task<bool> SetPragmaAsync(string name, object value);

    Task ShutdownAsync(bool forced);

}
