using System.Data;
using Dapper;
using Elsa.Dapper.Models;

namespace Elsa.Dapper.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ParameterizedQuery"/> to execute queries.
/// </summary>
public static class ParameterizedQueryExtensions
{
    /// <summary>
    /// Execute a query asynchronously using Task.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="connection">The connection to query on.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <typeparam name="T">The type to return.</typeparam>
    /// <returns>A single row of the query result.</returns>
    public static async Task<T?> SingleAsync<T>(this ParameterizedQuery query, IDbConnection connection, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        return await connection.QuerySingleAsync<T>(query.Sql.ToString(), query.Parameters, transaction, commandTimeout, commandType);
    }
    
    /// <summary>
    /// Execute a single-row query asynchronously using Task.
    /// </summary>
    /// <typeparam name="T">The type to return.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="connection">The connection to query on.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <returns>A single row of the query result.</returns>
    public static async Task<T?> SingleOrDefaultAsync<T>(this ParameterizedQuery query, IDbConnection connection, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        return await connection.QuerySingleOrDefaultAsync<T>(query.Sql.ToString(), query.Parameters, transaction, commandTimeout, commandType);
    }
    
    /// <summary>
    /// Execute a single-row query asynchronously using Task.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="connection">The connection to query on.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <typeparam name="T">The type to return.</typeparam>
    /// <returns>A single row of the query result.</returns>
    public static async Task<T?> FirstOrDefaultAsync<T>(this ParameterizedQuery query, IDbConnection connection, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        return await connection.QueryFirstOrDefaultAsync<T>(query.Sql.ToString(), query.Parameters, transaction, commandTimeout, commandType);
    }
    
    /// <summary>
    /// Execute a query asynchronously using Task.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="connection">The connection to query on.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <typeparam name="T">The type to return.</typeparam>
    /// <returns>A sequence of data of the supplied type.</returns>
    public static async Task<IEnumerable<T>> QueryAsync<T>(this ParameterizedQuery query, IDbConnection connection, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        return await connection.QueryAsync<T>(query.Sql.ToString(), query.Parameters, transaction, commandTimeout, commandType);
    }

    /// <summary>
    /// Execute a command asynchronously using Task.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="connection">The connection to query on.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    public static async Task<int> ExecuteAsync(this ParameterizedQuery query, IDbConnection connection, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        return await connection.ExecuteAsync(query.Sql.ToString(), query.Parameters, transaction, commandTimeout, commandType);
    }
}