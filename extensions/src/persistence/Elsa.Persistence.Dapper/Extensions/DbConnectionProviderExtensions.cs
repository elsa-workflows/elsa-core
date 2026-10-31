using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Models;

namespace Elsa.Persistence.Dapper.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IDbConnectionProvider"/>.
/// </summary>
public static class DbConnectionProviderExtensions
{
    /// <summary>
    /// Creates a <see cref="ParameterizedQuery"/> instance.
    /// </summary>
    /// <param name="dbConnectionProvider">The <see cref="IDbConnectionProvider"/> instance.</param>
    /// <returns>A <see cref="ParameterizedQuery"/> instance.</returns>
    public static ParameterizedQuery CreateQuery(this IDbConnectionProvider dbConnectionProvider) => new(dbConnectionProvider.Dialect);
}