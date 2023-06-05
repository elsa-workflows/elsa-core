using System.Data;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Dialects;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;

namespace Elsa.Dapper.Services;

/// <summary>
/// Provides a SQLite connection to the database.
/// </summary>
[PublicAPI]
public class SqliteDbConnectionProvider : IDbConnectionProvider
{
    /// <inheritdoc />
    public string GetConnectionString() => "Data Source=elsa.dapper.db";

    /// <inheritdoc />
    public IDbConnection GetConnection()
    {
        return new SqliteConnection
        {
            ConnectionString = GetConnectionString()
        };
    }

    /// <inheritdoc />
    public ISqlDialect Dialect => new SqliteDialect();
}