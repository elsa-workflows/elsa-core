using MySql.Data.MySqlClient;
using Elsa.Sql.Client;
using System.Data;

namespace Elsa.Sql.MySql;

public class MySqlClient : BaseSqlClient, ISqlClient
{
    private string? _connectionString;

    /// <summary>
    /// MySql client implimentation.
    /// </summary>
    /// <param name="connectionString"></param>
    public MySqlClient(string? connectionString) => _connectionString = connectionString;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<int?> ExecuteCommandAsync(string sqlCommand)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        var command = new MySqlCommand(sqlCommand, connection);

        var result = await command.ExecuteNonQueryAsync();
        return result;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(string sqlQuery)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        var command = new MySqlCommand(sqlQuery, connection);

        var result = await command.ExecuteScalarAsync();
        return result;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<DataSet?> ExecuteQueryAsync(string sqlQuery)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        var command = new MySqlCommand(sqlQuery, connection);

        using var reader = await command.ExecuteReaderAsync();
        return await Task.FromResult(ReadAsDataSet(reader));
    }
}