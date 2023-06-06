using System.Text;
using Dapper;
using Elsa.Dapper.Contracts;

namespace Elsa.Dapper.Models;

/// <summary>
/// Represents a parameterized SQL query.
/// </summary>
public class ParameterizedQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterizedQuery"/> class.
    /// </summary>
    /// <param name="dialect">The SQL dialect.</param>
    public ParameterizedQuery(ISqlDialect dialect)
    {
        Dialect = dialect;
    }
    
    /// <summary>
    /// Gets the SQL dialect.
    /// </summary>
    public ISqlDialect Dialect { get; }

    /// <summary>A <see cref="StringBuilder"/> containing the SQL query.</summary>
    public StringBuilder Sql { get; } = new();

    /// <summary>A <see cref="DynamicParameters"/> containing the parameters.</summary>
    public DynamicParameters Parameters { get; } = new();
}