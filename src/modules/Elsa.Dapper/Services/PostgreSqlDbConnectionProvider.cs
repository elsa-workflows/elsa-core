using System.Data;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Dialects;
using JetBrains.Annotations;
using Npgsql;

namespace Elsa.Dapper.Services;

/// <summary>
/// Provides a PostgreSql connection to the database.
/// </summary>
[PublicAPI]
public class PostgreSqlDbConnectionProvider : IDbConnectionProvider
{
    private readonly string _connectionString = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDbConnectionProvider"/> class.
    /// </summary>
    public PostgreSqlDbConnectionProvider()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDbConnectionProvider"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string to use.</param>
    public PostgreSqlDbConnectionProvider(string connectionString)
    {
        _connectionString = connectionString;
    }


    /// <inheritdoc />
    public string GetConnectionString() => _connectionString;

    /// <inheritdoc />
    public IDbConnection GetConnection()
    {
        return new NpgsqlConnection
        {
            ConnectionString = GetConnectionString()
        };
    }

    /// <inheritdoc />
    public ISqlDialect Dialect => new PostgreSqlDialect();
}