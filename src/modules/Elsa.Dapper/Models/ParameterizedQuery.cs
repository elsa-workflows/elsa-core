using System.Text;
using Dapper;
using Elsa.Dapper.Contracts;

namespace Elsa.Dapper.Models;

/// <summary>
/// Represents a parameterized SQL query.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ParameterizedQuery"/> class.
/// </remarks>
/// <param name="dialect">The SQL dialect.</param>
public class ParameterizedQuery(ISqlDialect dialect)
{

    /// <summary>
    /// Gets the SQL dialect.
    /// </summary>
    public ISqlDialect Dialect { get; } = dialect;

    /// <summary>A <see cref="StringBuilder"/> containing the SQL query.</summary>
    public StringBuilder Sql { get; } = new();

    /// <summary>A <see cref="DynamicParameters"/> containing the parameters.</summary>
    public DynamicParameters Parameters { get; } = new();
}