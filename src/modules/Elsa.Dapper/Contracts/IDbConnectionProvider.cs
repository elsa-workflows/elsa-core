using System.Data;

namespace Elsa.Dapper.Contracts;

/// <summary>
/// Provides a connection to the database and corresponding SQL dialect.
/// </summary>
public interface IDbConnectionProvider
{
    /// <summary>
    /// Gets the connection string.
    /// </summary>
    /// <returns>The connection string.</returns>
    string GetConnectionString();
    
    /// <summary>
    /// Gets a connection to the database.
    /// </summary>
    /// <returns>A connection to the database.</returns>
    IDbConnection GetConnection();
    
    /// <summary>
    /// Gets the SQL dialect.
    /// </summary>
    ISqlDialect Dialect { get; }
}