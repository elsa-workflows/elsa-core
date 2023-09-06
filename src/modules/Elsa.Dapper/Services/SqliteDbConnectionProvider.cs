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
    private readonly string _connectionString = "Data Source=elsa.dapper.db";

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDbConnectionProvider"/> class.
    /// </summary>
    public SqliteDbConnectionProvider()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDbConnectionProvider"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string to use.</param>
    public SqliteDbConnectionProvider(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    /// <inheritdoc />
    public string GetConnectionString() =>_connectionString;

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