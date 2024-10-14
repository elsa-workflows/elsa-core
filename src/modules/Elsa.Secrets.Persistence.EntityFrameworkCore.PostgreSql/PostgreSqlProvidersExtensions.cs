using System.Reflection;
using Elsa.Secrets.Persistence.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use PostgreSql.
/// </summary>
public static class SecretsPostgreSqlProvidersExtensions
{
    private static Assembly Assembly => typeof(SecretsPostgreSqlProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the feature to use PostgreSql.
    /// </summary>
    public static EFCoreSecretPersistenceFeature UsePostgreSql(this EFCoreSecretPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = null)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(Assembly, connectionString, options);
        return feature;
    }
}