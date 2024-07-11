using System.Data;
using Dapper;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Dialects;
using Elsa.Dapper.TypeHandlers.Sqlite;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;

namespace Elsa.Dapper.Services;

/// <summary>
/// Provides a SQLite connection to the database.
/// </summary>
[PublicAPI]
public class SqliteDbConnectionProvider : IDbConnectionProvider
{
    private readonly string _connectionString = "Data Source=:memory:;Cache=Shared";

    static SqliteDbConnectionProvider()
    {
        // See: https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/dapper-limitations#data-types
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
    }

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
    public string GetConnectionString() => _connectionString;

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