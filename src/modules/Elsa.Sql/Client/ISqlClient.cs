using System.Data;

namespace Elsa.Sql.Client;

public interface ISqlClient
{
    /// <summary>
    /// Asynchronously executes a Transact-SQL statement against the connection and returns the number of rows affected.
    /// </summary>
    /// <param name="sqlCommand">The command to execute</param>
    /// <returns>The number of rows affected.</returns>
    public Task<int?> ExecuteCommandAsync(string sqlCommand);

    /// <summary>
    /// Asynchronously executes the query, and returns the first column of the first row in the result set returned by the query. Additional columns or rows are ignored.
    /// </summary>
    /// <param name="sqlQuery">The query to execute</param>
    /// <returns>The first column of the first row in the result set, or a null reference if the result set is empty. Returns a maximum of 2033 characters.</returns>
    public Task<object?> ExecuteScalarAsync(string sqlQuery);

    /// <summary>
    /// Asynchronously executes the query, and returns a dataset of data returned by the query.
    /// </summary>
    /// <param name="sqlQuery">Query to execute</param>
    /// <returns>DataSet of the queried data</returns>
    public Task<DataSet?> ExecuteQueryAsync(string sqlQuery);
}