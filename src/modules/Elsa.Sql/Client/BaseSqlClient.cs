using System.Data;
using System.Data.Common;
using Elsa.Sql.Models;

namespace Elsa.Sql.Client;

public abstract class BaseSqlClient : ISqlClient
{
    /// <summary>
    /// The connection string used to connect with the database.
    /// </summary>
    protected readonly string _connectionString;

    /// <summary>
    /// The marker used when injecting parameters into a query.
    /// Default: "@"
    /// </summary>
    public virtual string ParameterMarker { get; set; } =  "@";

    /// <summary>
    /// The text following the <c>ParameterMarker</c> when injecting parameters into a query.
    /// Default: "param"
    /// </summary>
    public virtual string ParameterText { get; set; } = "p";

    /// <summary>
    /// Set to true to add a counter to the end of the parameter string.
    /// Default: true
    /// </summary>
    public virtual bool IncrementParameter { get; set; } = true;

    /// <summary>
    /// Default base implementation for an SQL client.
    /// </summary>
    /// <param name="connectionString"></param>
    protected BaseSqlClient(string connectionString) => _connectionString = connectionString;

    /// <summary>
    /// Create a connection using the client specific connection.
    /// </summary>
    /// <returns></returns>
    protected abstract DbConnection CreateConnection();

    /// <summary>
    /// Create a command using the client specific connection.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="connection"></param>
    /// <returns></returns>
    protected abstract DbCommand CreateCommand(string query, DbConnection connection);

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<int?> ExecuteCommandAsync(EvaluatedQuery evaluatedQuery)
    {
        using var connection = CreateConnection();
        connection.Open();
        var command = CreateCommand(evaluatedQuery.Query, connection);
        AddCommandParameters(command, evaluatedQuery.Parameters);

        var result = await command.ExecuteNonQueryAsync();
        return result;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(EvaluatedQuery evaluatedQuery)
    {
        using var connection = CreateConnection();
        connection.Open();
        var command = CreateCommand(evaluatedQuery.Query, connection);
        AddCommandParameters(command, evaluatedQuery.Parameters);

        var result = await command.ExecuteScalarAsync();
        return result;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<DataSet?> ExecuteQueryAsync(EvaluatedQuery evaluatedQuery)
    {
        using var connection = CreateConnection();
        connection.Open();
        var command = CreateCommand(evaluatedQuery.Query, connection);
        AddCommandParameters(command, evaluatedQuery.Parameters);

        using var reader = await command.ExecuteReaderAsync();
        return await Task.FromResult(ReadAsDataSet(reader));
    }

    /// <summary>
    /// Add parameters into the query to prevent SQL injection.
    /// </summary>
    /// <param name="command">Command to add the parameters to</param>
    /// <param name="parameters">Parameters to add</param>
    /// <returns></returns>
    private DbCommand AddCommandParameters(DbCommand command, Dictionary<string, object?> parameters)
    {
        foreach (var param in parameters)
        {
            var dbParam = command.CreateParameter();
            dbParam.ParameterName = param.Key;
            dbParam.Value = param.Value ?? DBNull.Value;
            command.Parameters.Add(dbParam);
        }
        return command;
    }

    /// <summary>
    /// Returns <see cref="IDataReader"/> data as a <see cref="DataSet"/>.
    /// </summary>
    /// <param name="reader">Reader to return data from.</param>
    /// <returns><see cref="DataSet"/> of data.</returns>
    private DataSet ReadAsDataSet(IDataReader reader)
    {
        var dataSet  = new DataSet("dataset");
        dataSet.Tables.Add(ReadAsDataTable(reader));
        return dataSet;
    }

    /// <summary>
    /// Returns <see cref="IDataReader"/> data as a <see cref="DataTable"/>.
    /// </summary>
    /// <param name="reader">Reader to return data from.</param>
    /// <returns><see cref="DataTable"/> of data.</returns>
    private DataTable ReadAsDataTable(IDataReader reader)
    {
        var data = new DataTable();
        var schemaTable =reader.GetSchemaTable();

        foreach (DataRow row in schemaTable.Rows)
        {
            string colName = row.Field<string>("ColumnName");
            Type t = row.Field<Type>("DataType");
            data.Columns.Add(colName, t);
        }

        while (reader.Read())
        {
            var newRow = data.Rows.Add();
            foreach (DataColumn col in data.Columns)
            {
                newRow[col.ColumnName] = reader[col.ColumnName];
            }
        }
        return data;
    }
}