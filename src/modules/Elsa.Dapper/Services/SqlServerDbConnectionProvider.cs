using System.Data;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Dialects;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;

namespace Elsa.Dapper.Services;

/// <summary>
/// Provides a SQLite connection to the database.
/// </summary>
[PublicAPI]
public class SqlServerDbConnectionProvider : IDbConnectionProvider
{
    private readonly string _connectionString = "Server=localhost;Database=Elsa;Trusted_Connection=True;";

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDbConnectionProvider"/> class.
    /// </summary>
    public SqlServerDbConnectionProvider()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDbConnectionProvider"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string to use.</param>
    public SqlServerDbConnectionProvider(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    /// <inheritdoc />
    public string GetConnectionString() =>_connectionString;

    /// <inheritdoc />
    public IDbConnection GetConnection()
    {
        return new SqlConnection
        {
            ConnectionString = GetConnectionString()
        };
    }

    /// <inheritdoc />
    public ISqlDialect Dialect => new SqliteDialect();
}