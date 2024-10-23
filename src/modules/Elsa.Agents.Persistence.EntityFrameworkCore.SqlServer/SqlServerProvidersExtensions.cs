using System.Reflection;
using Elsa.Agents.Persistence.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use SQL Server.
/// </summary>
public static class AgentsSqlServerProvidersExtensions
{
    private static Assembly Assembly => typeof(AgentsSqlServerProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the feature to use SQL Server.
    /// </summary>
    public static EFCoreAgentPersistenceFeature UseSqlServer(this EFCoreAgentPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = null)
    {
        feature.UseSqlServer(Assembly, connectionString, options);
        return feature;
    }
}