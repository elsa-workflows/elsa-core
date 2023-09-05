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
    /// <inheritdoc />
    public string GetConnectionString() => "Data Source=elsa.dapper.db";

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