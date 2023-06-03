using System.Data;

namespace Elsa.Dapper.Contracts;

/// <summary>
/// Provides a connection to the database.
/// </summary>
public interface IDbConnectionProvider
{
    /// <summary>
    /// Gets a connection to the database.
    /// </summary>
    /// <returns>A connection to the database.</returns>
    IDbConnection GetConnection();
}