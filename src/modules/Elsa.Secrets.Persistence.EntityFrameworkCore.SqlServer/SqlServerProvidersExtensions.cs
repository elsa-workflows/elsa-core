using System.Reflection;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Secrets.Persistence.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

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