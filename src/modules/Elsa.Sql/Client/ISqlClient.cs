using System.Data;
using Elsa.Sql.Models;

namespace Elsa.Sql.Client;

public interface ISqlClient
{
    /// <summary>
    /// Asynchronously executes a Transact-SQL statement against the connection and returns the number of rows affected.
    /// </summary>
    /// <param name="evaluatedQuery">The evaluated query to execute.</param>
    /// <returns>The number of rows affected.</returns>
    public Task<int?> ExecuteCommandAsync(EvaluatedQuery evaluatedQuery);

    /// <summary>
    /// Asynchronously executes the query, and returns the first column of the first row in the result set returned by the query. Additional columns or rows are ignored.
    /// </summary>
    /// <param name="evaluatedQuery">The evaluated query to execute.</param>
    /// <returns>The first column of the first row in the result set, or a null reference if the result set is empty. Returns a maximum of 2033 characters.</returns>
    public Task<object?> ExecuteScalarAsync(EvaluatedQuery evaluatedQuery);

    /// <summary>
    /// Asynchronously executes the query, and returns a dataset of data returned by the query.
    /// </summary>
    /// <param name="evaluatedQuery">The evaluated query to execute.</param>
    /// <returns>DataSet of the queried data</returns>
    public Task<DataSet?> ExecuteQueryAsync(EvaluatedQuery evaluatedQuery);
}