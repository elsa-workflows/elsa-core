using System.Reflection;
using Elsa.Agents.Persistence.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use SQL Server.
/// </summary>
public static class AgentsPostgreSqlProvidersExtensions
{
    private static Assembly Assembly => typeof(AgentsPostgreSqlProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the feature to use SQL Server.
    /// </summary>
    public static EFCoreAgentPersistenceFeature UsePostgreSql(this EFCoreAgentPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = null)
    {
        feature.UsePostgreSql(Assembly, connectionString, options);
        return feature;
    }
}