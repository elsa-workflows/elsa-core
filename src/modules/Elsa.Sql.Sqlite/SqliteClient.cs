using System.Data;
using Elsa.Sql.Client;
using Microsoft.Data.Sqlite;

namespace Elsa.Sql.Sqlite;

public class SqliteClient : BaseSqlClient, ISqlClient
{
    private string? _connectionString;

    /// <summary>
    /// Sqlite client implimentation.
    /// </summary>
    /// <param name="connectionString"></param>
    public SqliteClient(string? connectionString) => _connectionString = connectionString;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<int?> ExecuteCommandAsync(string sqlCommand)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = new SqliteCommand(sqlCommand, connection);

        var result = await command.ExecuteNonQueryAsync();
        return result;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(string sqlQuery)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = new SqliteCommand(sqlQuery, connection);

        var result = await command.ExecuteScalarAsync();
        return result;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<DataSet?> ExecuteQueryAsync(string sqlQuery)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = new SqliteCommand(sqlQuery, connection);

        using var reader = await command.ExecuteReaderAsync();
        return await Task.FromResult(ReadAsDataSet(reader));
    }
}