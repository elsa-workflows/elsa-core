using System.Reflection;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Secrets.Persistence.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use Sqlite.
/// </summary>
public static class SecretsSqliteProvidersExtensions
{
    private static Assembly Assembly => typeof(SecretsSqliteProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static EFCoreSecretPersistenceFeature UseSqlite(this EFCoreSecretPersistenceFeature feature, string? connectionString = null, ElsaDbContextOptions? options = null)
    {
        feature.UseSqlite(Assembly, connectionString, options);
        return feature;
    }
}