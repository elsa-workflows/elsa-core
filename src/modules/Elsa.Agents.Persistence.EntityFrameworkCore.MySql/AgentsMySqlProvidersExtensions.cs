using System.Reflection;
using Elsa.Agents.Persistence.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use MySQL.
/// </summary>
public static class AgentsMySqlProvidersExtensions
{
    private static Assembly Assembly => typeof(AgentsMySqlProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the feature to use MySQL.
    /// </summary>
    public static EFCoreAgentPersistenceFeature UseMySql(this EFCoreAgentPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = null)
    {
        feature.UseMySql(Assembly, connectionString, options);
        return feature;
    }
}