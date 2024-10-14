using System.Reflection;
using Elsa.Agents.Persistence.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use Sqlite.
/// </summary>
public static class AgentsSqliteProvidersExtensions
{
    private static Assembly Assembly => typeof(AgentsSqliteProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static EFCoreAgentPersistenceFeature UseSqlite(this EFCoreAgentPersistenceFeature feature, string? connectionString = null, ElsaDbContextOptions? options = null)
    {
        feature.UseSqlite(Assembly, connectionString, options);
        return feature;
    }
}