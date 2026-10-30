using System.Data;
using Dapper;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Dialects;
using Elsa.Persistence.Dapper.TypeHandlers.PostgreSql;
using JetBrains.Annotations;
using Npgsql;

namespace Elsa.Persistence.Dapper.Services;

/// <summary>
/// Provides a PostgreSql connection to the database.
/// </summary>
[PublicAPI]
public class PostgreSqlDbConnectionProvider : IDbConnectionProvider
{
    private readonly string _connectionString = "";

    static PostgreSqlDbConnectionProvider()
    {
        // Last AddTypeHandler for DateTimeOffset wins. Replace the SQLite string-only
        // handler so Npgsql DateTime values can be read back as DateTimeOffset.
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
    }

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