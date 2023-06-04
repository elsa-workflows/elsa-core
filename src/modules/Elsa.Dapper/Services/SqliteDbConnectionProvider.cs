using System.Data;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Dialects;
using Microsoft.Data.Sqlite;

namespace Elsa.Dapper.Services;

/// <summary>
/// Provides a SQLite connection to the database.
/// </summary>
public class SqliteDbConnectionProvider : IDbConnectionProvider
{
    /// <inheritdoc />
    public IDbConnection GetConnection()
    {
        return new SqliteConnection
        {
            ConnectionString = "Data Source=elsa.db"
        };
    }

    /// <inheritdoc />
    public ISqlDialect Dialect => new SqliteDialect();
}