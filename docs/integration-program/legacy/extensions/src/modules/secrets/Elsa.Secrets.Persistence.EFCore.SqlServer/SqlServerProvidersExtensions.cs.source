using System.Reflection;
using Elsa.Secrets.Persistence.EFCore;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use SqlServer.
/// </summary>
public static class SecretsSqlServerProvidersExtensions
{
    private static Assembly Assembly => typeof(SecretsSqlServerProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the feature to use SqlServer.
    /// </summary>
    public static EFCoreSecretPersistenceFeature UseSqlServer(this EFCoreSecretPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = null)
    {
        feature.UseSqlServer(Assembly, connectionString, options);
        return feature;
    }
}